# Architecture Patterns

**Domain:** macOS screen capture and sharing app (C# / Avalonia UI)
**Researched:** 2026-03-25
**Confidence:** MEDIUM-HIGH (Apple API architecture HIGH, C#/.NET interop layer MEDIUM)

---

## Recommended Architecture

The system splits cleanly into four horizontal layers. Each layer communicates only downward; no layer calls back up the stack except through defined callback interfaces.

```
┌─────────────────────────────────────────────────────────────┐
│  SHELL LAYER                                                │
│  AvaloniaUI Window  |  TrayIcon + NativeMenu  |  Hotkeys   │
└────────────────────────────┬────────────────────────────────┘
                             │ commands / events
┌────────────────────────────▼────────────────────────────────┐
│  WORKFLOW LAYER                                             │
│  TaskManager  |  WorkflowConfig  |  AfterCaptureQueue      │
└─────────┬────────────────┬───────────────────┬─────────────┘
          │                │                   │
┌─────────▼──────┐  ┌──────▼──────┐  ┌────────▼────────────┐
│ CAPTURE LAYER  │  │ EDIT LAYER  │  │ UPLOAD LAYER        │
│ SCKitBridge    │  │ ImageEditor │  │ UploadersLib        │
│ RegionSelector │  │ Annotator   │  │ UploadQueue         │
│ RecordingMgr   │  │ GifEncoder  │  │ HistoryLib          │
└─────────┬──────┘  └─────────────┘  └─────────────────────┘
          │
┌─────────▼────────────────────────────────────────────────────┐
│  NATIVE INTEROP LAYER                                        │
│  ScreenCaptureKit (P/Invoke)  |  CGEventTap / SharpHook      │
│  TCC Permission Manager       |  AVAssetWriter bridge        │
│  NSPasteboard bridge          |  Objective-C runtime stubs   │
└──────────────────────────────────────────────────────────────┘
```

---

## Component Boundaries

### Shell Layer

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| `MainWindow` (Avalonia) | Full settings UI, capture history grid, configuration panels | TaskManager (commands), HistoryLib (read) |
| `TrayIcon + NativeMenu` | Menu bar presence, quick-capture menu items | TaskManager (trigger captures), MainWindow (show/hide) |
| `HotkeyService` | Register and dispatch global hotkeys | TaskManager (fire capture task), SharpHook (underlying hook) |
| `PermissionsGate` | Check and request Screen Recording + Accessibility permissions at launch | Native Interop Layer (TCC API) |

**Key constraint:** `TrayIcon.Clicked` is NOT fired on macOS in Avalonia. The menu bar icon is purely menu-driven. All quick actions must be NativeMenu items. (MEDIUM confidence — confirmed by Avalonia community discussions as of 2025.)

---

### Workflow Layer

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| `TaskManager` | Orchestrates a full capture → edit → upload pipeline per task | CaptureLayer, EditLayer, UploadLayer |
| `WorkflowConfig` | Per-workflow settings: what to capture, after-capture actions, destination | Persisted to JSON; read by TaskManager |
| `AfterCaptureQueue` | Sequences post-capture steps (save, copy, edit, upload) | Calls each step handler in order |
| `CaptureHistory` | Records every task result with thumbnail, URL, local path | HistoryLib (storage), MainWindow (display) |

This is the direct port of ShareX's `TaskHelpers.cs` + `WorkflowConfig` pattern. The after-capture queue is the critical join point where the output of Capture feeds Edit and then Upload.

---

### Capture Layer

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| `SCKitBridge` | Wraps ScreenCaptureKit: enumerate displays/windows, take screenshots, start/stop streams | Native Interop Layer (P/Invoke to SCKit ObjC runtime) |
| `RegionSelector` | Fullscreen Avalonia overlay window for user to drag-select a capture region | Returns CGRect to TaskManager; reads display list from SCKitBridge |
| `RecordingManager` | Manages an active SCStream + AVAssetWriter session for video/GIF capture | SCKitBridge (stream control), AVAssetWriterBridge (frame writing) |
| `GifEncoder` | Converts captured video frames to animated GIF | Receives frame sequence from RecordingManager |

**Data contract:** Capture outputs one of: `CGImage` (screenshot) or `NSUrl` pointing to a written video/GIF file.

---

### Edit Layer

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| `ImageEditor` | Annotation canvas: arrows, text boxes, highlights, blur | Receives `CGImage` or `SKBitmap`; returns annotated bitmap |
| `ImageEffects` | Non-interactive effects: watermark, border, resize | Applied in AfterCaptureQueue before upload |

The original ShareX `ShareX.ImageEditor` and `ShareX.ImageEffectsLib` are candidates for direct reuse here — they are UI-framework-agnostic as long as GDI+ calls are replaced with SkiaSharp equivalents (Avalonia already uses Skia internally).

---

### Upload Layer

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| `UploadersLib` | All upload destinations: Imgur, S3, GCS, R2, custom endpoints | HttpClient (outbound); returns public URL or error |
| `UploadQueue` | Serializes concurrent uploads, handles retry/backoff | UploadersLib, CaptureHistory (update status) |
| `ClipboardService` | Writes image or URL to `NSPasteboard` | Native Interop Layer (NSPasteboard P/Invoke) |

---

### Native Interop Layer

This is the highest-risk layer and the primary departure from the original ShareX codebase.

| Component | Responsibility | API Used | Confidence |
|-----------|---------------|----------|------------|
| `ScreenCaptureKitInterop` | P/Invoke bindings or ObjC runtime calls to `SCShareableContent`, `SCStream`, `SCScreenshotManager` | ScreenCaptureKit (macOS 12.3+) | MEDIUM — dotnet/macios bindings exist but direct usage in standalone .NET apps (not MAUI) requires manual ObjC runtime interop |
| `TccPermissionManager` | Check `CGPreflightScreenCaptureAccess()`, prompt with `CGRequestScreenCaptureAccess()` | CoreGraphics TCC API | MEDIUM |
| `HotkeyInterop` | libuiohook wrapper via SharpHook NuGet package | SharpHook 5.x (macOS 10.15+, requires Accessibility permission) | HIGH — confirmed used by XerahS |
| `AVAssetWriterBridge` | Write CMSampleBuffers from SCStream to QuickTime/.mp4 file | AVFoundation AVAssetWriter | MEDIUM |
| `ClipboardInterop` | Write NSImage / file URL to NSPasteboard | AppKit NSPasteboard | MEDIUM |

---

## Data Flow

### Screenshot Capture Flow

```
User presses hotkey
    → SharpHook fires callback → HotkeyService
    → HotkeyService calls TaskManager.ExecuteTask(WorkflowConfig)
    → TaskManager evaluates capture mode:

  [Full screen]
    → SCKitBridge.CaptureDisplay(displayId)
    → SCScreenshotManager.captureImage(filter, config)
    → Returns CGImage
    → AfterCaptureQueue begins

  [Region]
    → RegionSelector opens fullscreen transparent Avalonia overlay
    → User drags rect → RegionSelector returns CGRect
    → SCKitBridge.CaptureRect(rect)
    → Returns CGImage

  [Window]
    → SCKitBridge.GetWindows() → show picker
    → User selects → SCKitBridge.CaptureWindow(SCWindow)
    → Returns CGImage

→ AfterCaptureQueue(CGImage):
    1. [optional] ImageEditor opens → returns annotated image
    2. [optional] Save to local file → NSUrl
    3. [optional] Copy to NSPasteboard
    4. [optional] UploadersLib.Upload(image) → URL string
    5. CaptureHistory.Record(entry)
    6. Notification shown
```

### Video/GIF Recording Flow

```
User triggers record hotkey
    → TaskManager.StartRecording(WorkflowConfig)
    → RegionSelector (if region mode) → CGRect
    → RecordingManager.Start(filter, rect):
        ├── SCStream created with SCContentFilter + SCStreamConfiguration
        ├── AVAssetWriter created targeting temp file path
        ├── SCStreamOutput delegate receives CMSampleBuffers
        └── Each buffer → retime → AVAssetWriterInput.append()

User triggers stop hotkey
    → RecordingManager.Stop():
        ├── SCStream.stopCapture()
        ├── AVAssetWriter.finishWriting()
        └── Returns file NSUrl

→ AfterCaptureQueue(NSUrl):
    1. [optional] GifEncoder converts .mov → .gif
    2. [optional] Save / copy / upload
```

### Permission Check Flow (run at app launch)

```
App starts
    → PermissionsGate.Check():
        ├── CGPreflightScreenCaptureAccess() → bool
        │   └── if false → CGRequestScreenCaptureAccess() → system dialog
        └── AXIsProcessTrusted() → bool (for Accessibility / hotkeys)
            └── if false → AXIsProcessTrustedWithOptions(prompt: true)
                → system prompt to open Accessibility prefs

Permission granted → continue to normal startup
Permission denied  → show informational UI, disable capture/hotkey features
```

---

## Suggested Build Order

Dependencies flow from the bottom up. Each layer must be mostly stable before the layer above it can be developed reliably.

### Phase 1 — Native Interop Foundation
Build first because everything else depends on it.

1. `ScreenCaptureKitInterop` — P/Invoke to SCShareableContent + SCScreenshotManager
2. `TccPermissionManager` — permission check and prompt
3. `HotkeyInterop` via SharpHook — global hotkey backbone
4. Basic Avalonia app shell — main window + TrayIcon + NativeMenu skeleton

**Gate:** Can take a single full-screen screenshot and save it to disk from a console-style call. Permissions prompt works. Hotkey fires a log message.

### Phase 2 — Capture Layer
Build after native interop is confirmed stable.

5. `SCKitBridge` — full screenshot API (display, window, rect)
6. `RegionSelector` overlay window — transparent Avalonia window with drag selection
7. `RecordingManager` + `AVAssetWriterBridge` — video recording to file
8. `GifEncoder` — post-process recording to GIF

**Gate:** Can capture full screen, a dragged region, a specific window. Can record a region to .mp4 and convert to .gif.

### Phase 3 — Workflow + Shell
Build after capture is reliable.

9. `WorkflowConfig` — data model, JSON persistence
10. `TaskManager` + `AfterCaptureQueue` — pipeline orchestration
11. `HotkeyService` — connect SharpHook events to TaskManager
12. `MainWindow` full UI + settings panels
13. `TrayIcon` menu wired to TaskManager tasks

**Gate:** Hotkey captures, runs through configurable after-capture steps, shows in main window.

### Phase 4 — Edit Layer
Can be developed in parallel with Phase 3 (no hard dependency).

14. `ImageEditor` — annotation canvas (port from ShareX.ImageEditor with SkiaSharp)
15. `ImageEffects` — watermark, border, resize

**Gate:** Can open an image in editor, annotate, return for further processing.

### Phase 5 — Upload Layer
Build last because it's independently testable.

16. `ClipboardService` — NSPasteboard bridge
17. `UploadersLib` — Imgur, S3, GCS, R2, custom HTTP
18. `UploadQueue` — retry, concurrency management
19. `CaptureHistory` — HistoryLib port with thumbnail generation

**Gate:** Can upload a file to Imgur and copy URL to clipboard. History shows all past captures.

---

## Key Architecture Decisions

### ScreenCaptureKit over CGWindowList
ScreenCaptureKit (macOS 12.3+) is the required path. CGWindowListCreateImage is deprecated in macOS 15 (Sequoia). ScreenCaptureKit provides better performance, multi-display support, and the `SCContentSharingPicker` path that reduces permission friction.
(HIGH confidence — Apple official docs confirm deprecation.)

### SCContentSharingPicker for Window Capture (Recommended)
When the user picks a specific window to capture/record, using `SCContentSharingPicker` instead of building a custom window list UI means the system handles the permission prompt on behalf of the app — no separate Screen Recording permission dialog. For full-screen and region capture without the picker, the traditional Screen Recording TCC permission is still required.
(HIGH confidence — confirmed by Apple WWDC23 + macOS Sequoia behavior documented in 2024.)

### SharpHook for Global Hotkeys
SharpHook (wraps libuiohook) is the only credible cross-platform global hotkey library for .NET. `RegisterEventHotKey` (Carbon) is deprecated since macOS 10.8. `CGEventTap` requires Accessibility permission and is the underlying mechanism libuiohook uses anyway. XerahS (the closest existing reference project) uses SharpHook in production.
(HIGH confidence — confirmed by XerahS source and SharpHook documentation.)

### Avalonia TrayIcon + NativeMenu (Not Custom NSStatusBar)
Avalonia 11 ships `TrayIcon` + `NativeMenu` that maps correctly to `NSStatusBar`/`NSStatusItem` on macOS. Building a custom P/Invoke bridge to NSStatusBar is unnecessary overhead. The known limitation — `TrayIcon.Clicked` not firing on macOS — means the tray icon must rely entirely on the drop-down menu for actions.
(MEDIUM confidence — confirmed by Avalonia issue tracker + community discussions.)

### Transparent Avalonia Overlay for Region Selection
The region-selection UI (drag to select a region) uses a maximized, borderless, transparent Avalonia window. The critical pattern is `Background="{x:Null}"` (not `Background="Transparent"`) to allow click-through to the underlying desktop in non-interactive areas. This is a confirmed Avalonia-on-macOS pattern.
(MEDIUM confidence — confirmed by Avalonia community discussion #11911.)

### P/Invoke / ObjC Runtime for Apple API Calls
The dotnet/macios project (formerly Xamarin.Mac) provides official C# bindings for ScreenCaptureKit and AVFoundation, but those bindings are tied to the MAUI/Xamarin deployment model. For a standalone Avalonia app, calling Apple native APIs requires either:
- Direct ObjC runtime calls via `ObjCRuntime.Messaging` / `NativeLibrary.Load`
- Wrapping calls in a thin native dylib written in Swift/Obj-C and P/Invoking into it

The thin-native-dylib approach (option 2) is lower risk: the Swift/ObjC code handles all ScreenCaptureKit + AVFoundation calls and exposes a simple C-callable interface. The C# side P/Invokes to that dylib. This isolates Apple API churn from the C# codebase.
(MEDIUM confidence — architecture recommendation based on observed pattern in similar projects; no single authoritative C#-only guide found.)

---

## Scalability Considerations

| Concern | Current Scope | If Scope Grows |
|---------|--------------|----------------|
| Multi-display capture | SCShareableContent enumerates all SCDisplays | Already handled by ScreenCaptureKit |
| Multiple concurrent uploads | UploadQueue with semaphore | Queue already designed for concurrency |
| High frame-rate recording | SCStreamConfiguration.minimumFrameInterval controls rate | Metal/IOSurface-backed buffers are low-copy; performance should scale |
| Large capture history | HistoryLib with thumbnail + metadata; full files on disk | Add SQLite index if > 10k entries; thumbnails stay separate |
| Plugin/custom uploaders | WorkflowConfig supports custom HTTP uploader endpoints in ShareX | Formal plugin API is post-v1 |

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Using GDI+ or System.Drawing for Image Processing
ShareX's original ScreenCaptureLib and ImageEffectsLib use `System.Drawing` (GDI+), which is not supported on macOS in .NET 6+. Replace all `System.Drawing` calls with `SkiaSharp` — Avalonia already depends on it, so it's a zero-cost addition.
**Instead:** `SKBitmap`, `SKCanvas`, `SKImage` throughout the image pipeline.

### Anti-Pattern 2: Building UI for Window/Screen Selection from Scratch
ShareX builds its own window picker UI. On macOS with ScreenCaptureKit, `SCContentSharingPicker` provides a system-native picker that requires no custom UI and eliminates the per-use permission prompt on macOS Sequoia+.
**Instead:** Use SCContentSharingPicker for window-targeted captures; keep custom region selector only for free-drag-region capture.

### Anti-Pattern 3: Running SCStream Callbacks on the Main Thread
ScreenCaptureKit delivers `CMSampleBuffer` on a dedicated dispatch queue, not the main thread. Marshalling every frame to the main thread before writing to AVAssetWriter will cause frame drops.
**Instead:** Process buffers on the SCStream callback queue; only dispatch UI updates back to the main thread.

### Anti-Pattern 4: Polling for Permission State
Checking TCC permissions on a timer loop is unreliable and wasteful. macOS provides `CGPreflightScreenCaptureAccess()` for a synchronous check and `CGRequestScreenCaptureAccess()` for an async request.
**Instead:** Check once at startup, then respond to explicit user-triggered permission flows.

### Anti-Pattern 5: One Big MainWindow ViewModel
The original ShareX `MainForm` is a monolithic Windows Form. Splitting this into a `TrayMenuViewModel`, `HistoryViewModel`, `SettingsViewModel`, and `CaptureWorkflowViewModel` from the start prevents the ViewModel-sprawl that plagued the original.
**Instead:** One ViewModel per major view surface, composed by a top-level `AppViewModel`.

---

## Sources

- [ScreenCaptureKit — Apple Developer Documentation](https://developer.apple.com/documentation/screencapturekit/)
- [Capturing Screen Content in macOS — Apple](https://developer.apple.com/documentation/ScreenCaptureKit/capturing-screen-content-in-macos)
- [A look at ScreenCaptureKit on macOS Sonoma — Nonstrict (2023)](https://nonstrict.eu/blog/2023/a-look-at-screencapturekit-on-macos-sonoma/)
- [Recording to Disk with ScreenCaptureKit — Nonstrict (2023)](https://nonstrict.eu/blog/2023/recording-to-disk-with-screencapturekit/)
- [What's new in ScreenCaptureKit — WWDC23](https://developer.apple.com/videos/play/wwdc2023/10136/)
- [SCContentSharingPicker and macOS Sequoia permission behavior — mjtsai blog (2024)](https://mjtsai.com/blog/2024/08/08/sequoia-screen-recording-prompts-and-the-persistent-content-capture-entitlement/)
- [Avalonia Native Platform Interop Docs](https://docs.avaloniaui.net/docs/app-development/native-interop)
- [Avalonia TrayIcon Docs](https://docs.avaloniaui.net/docs/reference/controls/tray-icon)
- [SharpHook — Cross-platform global keyboard/mouse hook for .NET](https://github.com/TolikPylypchuk/SharpHook)
- [XerahS — ShareX Avalonia port architecture reference](https://github.com/ShareX/XerahS)
- [dotnet/macios — ScreenCaptureKit C# bindings](https://github.com/dotnet/macios/wiki/ScreenCaptureKit-macOS-xcode16.0-b1)
- [capture-screen-macos-csharp — GitHub example (P/Invoke approach)](https://github.com/eyaldar/capture-screen-macos-csharp)
- [Avalonia pass-through transparency pattern — Discussion #11911](https://github.com/AvaloniaUI/Avalonia/discussions/11911)
- [How can I use Mac/iOS APIs like EventKit — Avalonia Discussion #14017](https://github.com/AvaloniaUI/Avalonia/discussions/14017)
