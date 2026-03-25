# Domain Pitfalls

**Domain:** macOS screen capture / sharing app — C# + Avalonia UI port of ShareX
**Researched:** 2026-03-25
**Overall confidence:** MEDIUM-HIGH (verified via official Apple docs, GitHub issues, Avalonia docs)

---

## Critical Pitfalls

Mistakes that cause rewrites or major rework.

---

### Pitfall 1: ScreenCaptureKit Requires an Active NSRunLoop

**What goes wrong:** Any async ScreenCaptureKit call (`SCShareableContent.GetShareableContentAsync()`, `SCStream.StartCaptureAsync()`) hangs indefinitely when invoked from a plain .NET console app or a thread that lacks a macOS run loop. The await never resolves and the app freezes.

**Why it happens:** ScreenCaptureKit is an Objective-C framework built on `NSRunLoop`-backed dispatch. A standard .NET console host does not initialize `NSApplication` or start `NSRunLoop.Main`, so XPC callbacks from the capture daemon have nowhere to land.

**Consequences:** Every capture operation silently deadlocks. This is a non-obvious blocker that appears immediately but looks like a hang rather than a configuration error.

**Prevention:**
- Initialize the macOS application properly using `NSApplication.Init()` before invoking any ScreenCaptureKit API.
- In an Avalonia app, `AppBuilder.Configure<App>().UseMacOS()` handles this, but any off-thread usage of ScreenCaptureKit (background services, test harnesses) must marshal the call to the main thread via `Dispatcher.UIThread.InvokeAsync` or explicitly set up `NSRunLoop`.
- Never call ScreenCaptureKit APIs from a `Task.Run` background thread without proper run loop setup.

**Detection:** Awaiting `GetShareableContentAsync()` never completes. No exception is thrown. Check that `NSApplication.IsRunning` is `true` at the call site.

**Phase:** Must be addressed in Phase 1 (capture foundation). Getting this wrong blocks all subsequent capture work.

**Source:** https://github.com/dotnet/macios/issues/17350

---

### Pitfall 2: macOS Sequoia Monthly Screen Recording Permission Prompts

**What goes wrong:** On macOS 15 (Sequoia), the OS re-prompts users for screen recording permission on a recurring basis — initially weekly, changed to monthly in 15.1. There is **no developer API or entitlement to disable this**. Users experience repeated permission dialogs even after granting "Allow Always."

**Why it happens:** Apple introduced mandatory privacy re-authorization in Sequoia for all apps that capture screen content, regardless of which capture API they use. The `com.apple.security.persistent-content-capture` entitlement exists but is reserved exclusively for VNC-class apps (remote desktop); applying for it requires Apple approval via a special request form.

**Consequences:** Users of the app will see recurring OS permission dialogs. On macOS 15.0, this was weekly; 15.1 reduced it to monthly for apps the user "regularly uses." Enterprise users can suppress the prompt via MDM configuration profile, but consumer users cannot.

**Prevention:**
- Design the permission request flow gracefully: detect permission state at startup and guide the user through granting it with clear instructions.
- Do not treat a denied permission as a fatal error — show an actionable error state with a "Grant Permission" button that opens System Settings to the correct pane.
- Document this behavior clearly in the app's first-run experience so users are not alarmed by the recurring OS dialog.
- Do not attempt workarounds (e.g., setting system date forward); these are hacks, not solutions.

**Detection:** Users report being re-prompted after a period of use. On macOS 15+, this is expected behavior, not a bug.

**Phase:** Address permission UX/error handling in Phase 1. The recurring-prompt behavior is unavoidable — just handle it gracefully.

**Sources:**
- https://9to5mac.com/2024/08/06/macos-sequoia-screen-recording-privacy-prompt/
- https://www.idownloadblog.com/2024/10/09/macos-sequoia-15-1-macos-screen-recording-prompts-frequency-reduced/

---

### Pitfall 3: System.Drawing.Common / GDI+ Will Not Run on macOS

**What goes wrong:** The original ShareX codebase uses `System.Drawing` (GDI+) extensively — for image manipulation, annotation rendering, bitmap operations, and thumbnail generation. `System.Drawing.Common` throws `PlatformNotSupportedException` on non-Windows platforms starting with .NET 6, where it became a Windows-only library.

**Why it happens:** Microsoft officially deprecated `System.Drawing.Common` for non-Windows as a breaking change in .NET 6. The underlying `libgdiplus` implementation on macOS was incomplete and unmaintained. The framework now throws at runtime rather than silently producing wrong results.

**Consequences:** Any ShareX library code that imports `System.Drawing` will crash at runtime on macOS. This includes large portions of `ScreenCaptureLib`, `ImageEditor`, and `MediaLib`. This is not a compile-time error — it fails at runtime when the affected code paths are exercised.

**Prevention:**
- Audit all ShareX libraries for `System.Drawing` usage before porting. Map every usage to a replacement.
- Recommended replacement: **SkiaSharp** for rendering and drawing operations (used by Avalonia internally, so already available). For pure image I/O, **ImageSharp** (100% managed, no native deps).
- Do not use `System.Drawing` anywhere in the macOS build. Add `<RuntimeIdentifier>osx-arm64</RuntimeIdentifier>` targeted compile guards if needed.
- Avalonia's own rendering pipeline is Skia-based — lean into SkiaSharp to keep one rendering stack.

**Detection:** Runtime `PlatformNotSupportedException` on macOS when drawing or image code is invoked. Can also be detected at build time by adding an analyzer: `<EnableWindowsTargeting>false</EnableWindowsTargeting>`.

**Phase:** Must be addressed in Phase 1 (architecture/porting). This is the single largest porting blocker because it affects multiple ShareX libraries simultaneously.

**Source:** https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only

---

### Pitfall 4: Transparent Overlay Window for Region Selection Has No Click-Through in Avalonia

**What goes wrong:** The region selection UI (drag to select capture area) requires a fullscreen transparent window that passes mouse clicks through to underlying apps in empty areas, while still capturing drag events for the selection rectangle. Avalonia's XPF layer does not support per-pixel hit transparency on macOS — the entire window receives or ignores mouse events.

**Why it happens:** Avalonia's macOS window implementation does not expose the `NSWindow` `ignoresMouseEvents` / `isOpaque` combination needed for true click-through transparency. The framework's abstraction layer does not surface this native capability.

**Consequences:** A naive implementation either blocks all mouse input to other apps (bad UX during region selection) or ignores all mouse input in the overlay (selection doesn't work). Neither is acceptable for a region selection tool.

**Prevention:**
- Use P/Invoke to call native `NSWindow` APIs directly to set `ignoresMouseEvents: NO` on interactive areas and handle cursor events natively, or use an Objective-C helper (compiled into a native dylib) that Avalonia calls via interop.
- Alternative: Use Avalonia's `TopLevel` with `WindowTransparencyLevel.Transparent` and overlay interactive Avalonia controls only on the selection handle regions; handle the click-through problem for non-handle areas via native interop.
- Study how existing open-source macOS screen capturers (e.g., Kap, macOS `screencapture` CLI) implement 5-window selection overlays at the appropriate `NSWindowLevel`.

**Detection:** Region selection overlay either blocks input to other apps or doesn't respond to drag events. Test this explicitly against fullscreen apps and multiple spaces.

**Phase:** Phase 2 (region capture UI). Requires a clear architecture decision before implementation begins — do not attempt this with pure Avalonia controls.

**Sources:**
- https://github.com/AvaloniaUI/Avalonia/discussions/11911
- https://github.com/AvaloniaUI/Avalonia/discussions/13827

---

### Pitfall 5: App Sandbox Breaks Avalonia + Screen Recording + Accessibility

**What goes wrong:** Enabling the macOS App Sandbox (`com.apple.security.app-sandbox` entitlement) causes Avalonia apps to crash on M1/M2 Macs unless the app is published as a single file. Additionally, sandboxed apps face additional restrictions: XPC services do not inherit TCC permissions from the parent app, global hotkeys via Accessibility API fail in sandboxed contexts, and screen recording entitlement behavior changes.

**Why it happens:** Avalonia loads native code at runtime in ways that sandbox restrictions block. Additionally, macOS TCC does not propagate permissions from a sandboxed app to its XPC services — each must be authorized separately, and the consent dialog may not appear at all.

**Consequences:**
- App crashes on launch in sandbox mode on Apple Silicon unless single-file published.
- Global hotkeys silently stop working — no error, no permission dialog.
- Any XPC service architecture for capture fails silently.

**Prevention:**
- **Skip the App Store for v1** — avoid the sandbox entirely. Distribute via direct download or Homebrew cask with notarization.
- If sandbox becomes necessary later: publish as single file only (`PublishSingleFile=true`), and test all permission flows on a fresh macOS user account.
- Do not use XPC services for the capture pipeline in a sandboxed context.
- For global hotkeys, ensure the accessibility permission is granted and test on a clean system (not the development machine where it may already be cached).

**Detection:** App crash on launch on Apple Silicon (sandbox). Hotkeys work during development but fail on distribution builds. Use `Console.app` and filter by the app's bundle ID to see sandbox violation messages.

**Phase:** Phase 1 (distribution/signing setup). Make a sandbox decision early — it affects architecture throughout.

**Sources:**
- https://github.com/AvaloniaUI/Avalonia/issues/9764
- https://developer.apple.com/forums/thread/760112

---

## Moderate Pitfalls

---

### Pitfall 6: Code Signing Identity Instability Breaks TCC Permission Memory

**What goes wrong:** macOS TCC (Transparency, Consent, and Control) uses the app's code signature to identify it across versions. If the signing identity changes between builds — because the app is unsigned, ad-hoc signed, or signed with different certificates — TCC treats each build as a new app and forgets previously granted permissions. Users must re-authorize screen recording and accessibility on every install.

**Why it happens:** During development, .NET/Avalonia apps are often run without signing (`dotnet run`) or with ad-hoc signing. TCC cannot correlate an ad-hoc-signed build to a previously certificate-signed build.

**Prevention:**
- Establish a stable Developer ID certificate early (even free Apple Developer account for development). Use it consistently.
- For CI/CD, ensure the same certificate is used for all builds.
- Document the permission re-grant flow for beta testers who will receive builds with a new signing identity.

**Detection:** Users report needing to re-grant screen recording permission after every update. Check `tccutil reset ScreenCapture` behavior in testing.

**Phase:** Phase 1 (CI/signing pipeline setup).

**Source:** https://developer.apple.com/forums/thread/760483

---

### Pitfall 7: Avalonia Input Lag on macOS Trackpad Drag Events

**What goes wrong:** Avalonia does not group input events on macOS the same way the platform expects. Dragging UI elements (e.g., annotation handles, region selection corners) exhibits a quarter-second lag behind the actual pointer position on macOS. This does not occur on Windows.

**Why it happens:** macOS coalesces pointer events differently than Windows. Avalonia's event processing pipeline does not account for this coalescing, causing it to replay all intermediate positions instead of skipping to the current one during rapid movement.

**Consequences:** The image annotation editor and region selection UX feel sluggish on trackpad. This is a DX quality issue that users will notice and complain about.

**Prevention:**
- Test all drag interactions on physical macOS hardware with a trackpad early in development — do not rely solely on mouse input testing.
- Monitor Avalonia GitHub for fixes (this is a known open issue).
- For performance-critical drag paths (region selection), consider implementing the drag handling via native NSEvent tracking rather than Avalonia's event system.

**Detection:** Drag interactions feel visibly laggy compared to native macOS apps. Use Instruments' Time Profiler to confirm event processing delay.

**Phase:** Surfaces in Phase 2 (capture UI) and Phase 3 (annotation editor). Flag for UX testing in both phases.

**Source:** https://github.com/AvaloniaUI/Avalonia/discussions/11925

---

### Pitfall 8: ScreenCaptureKit Silent Frame Drop and Audio Capture Failure on macOS 15

**What goes wrong:** Under system load, `SCStream` drops frames without warning the delegate — the recording timer continues, the UI shows "recording," but the output file contains repeated frames or gaps. Separately, system audio capture via `SCStream` on macOS 15 fails with `SCStreamErrorDomain Code=-3805` (connection invalid) — the stream starts but stops almost immediately.

**Why it happens:** Frame drops occur because the frame queue is finite (default: 5 frames, max: 8). If the consumer (`didOutputSampleBuffer`) processes frames slower than the display refresh, frames are silently discarded with a log warning. The audio capture failure on macOS 15 is an Apple bug with no confirmed workaround yet.

**Prevention:**
- Implement `SCStreamDelegate.stream(_:didStopWithError:)` to detect unexpected stream termination and surface it to the user immediately.
- Process frames on a dedicated high-priority dispatch queue to minimize queue buildup.
- For audio capture, test on macOS 14 and 15 separately. Have a fallback: if `SCStream` audio capture fails, attempt `AVAudioEngine` with tap input as a workaround.
- Surface recording errors to the user immediately rather than continuing silently.

**Detection:** Playback of recorded video shows frozen frames or audio gaps. Check system logs for `_SCStream_Remote...QueueOperationHandlerWithError: Dropping frame` messages.

**Phase:** Phase 2 (video/GIF recording). Build error detection into the capture pipeline from the start.

**Sources:**
- https://github.com/ronaldoussoren/pyobjc/issues/647
- https://fatbobman.com/en/posts/screensage-from-pixel-to-meta/

---

### Pitfall 9: Notarization Requires JIT Entitlement for Non-AOT .NET Apps

**What goes wrong:** Notarization requires the hardened runtime (`--options=runtime`). The hardened runtime blocks JIT compilation by default. .NET apps (non-NativeAOT) require JIT to run, so without the `com.apple.security.cs.allow-jit` entitlement, the notarized app crashes immediately on launch.

**Why it happens:** The hardened runtime treats JIT code generation as a security risk. .NET's CLR uses JIT by default. Notarized apps without this entitlement get killed by the OS before any managed code runs.

**Prevention:**
- Always include `com.apple.security.cs.allow-jit` in the entitlements file for non-NativeAOT builds.
- Required minimum entitlements.plist for .NET + notarization:
  ```xml
  <key>com.apple.security.cs.allow-jit</key><true/>
  ```
- Do **not** add `com.apple.security.cs.allow-unsigned-executable-memory` (no longer required since .NET 6.0.1, and it weakens security posture).
- Build and notarize on a Mac — the `codesign` and `notarytool` commands require Xcode and cannot be run on Windows/Linux.

**Detection:** App launches locally (unsigned/ad-hoc) but immediately terminates on another machine after installation. `Console.app` shows `killed` or `code signature` errors.

**Phase:** Phase 1 (distribution pipeline). Set this up before the first distribution build.

**Sources:**
- https://learn.microsoft.com/en-us/dotnet/core/install/macos-notarization-issues
- https://www.kenmuse.com/blog/notarizing-dotnet-console-apps-for-macos/

---

### Pitfall 10: Global Hotkeys Regressed on macOS 15.4

**What goes wrong:** Global hotkeys registered via the Accessibility API stopped working in macOS 15.4 for some apps — hotkeys only fire when the application is in the foreground. Resetting accessibility permissions does not resolve this for affected users.

**Why it happens:** Apple changed event tap behavior in 15.4 (details not yet documented). This is an OS-level regression affecting multiple apps (confirmed in Ghostty issue tracker).

**Prevention:**
- Design the app to degrade gracefully when hotkeys fail: surface a status indicator showing whether global hotkeys are active.
- Provide an in-app fallback trigger (menu bar click + action) for all hotkey-accessible features.
- Monitor Apple developer forums and the relevant GitHub issues for patch releases.
- Test hotkey registration on clean macOS installs at each macOS point release.

**Detection:** Hotkeys work in development but fail on user machines running 15.4+. No error is thrown — hotkeys simply do not fire globally.

**Phase:** Phase 2 (global hotkey system). Build fallback mechanisms into the design from day one.

**Source:** https://github.com/ghostty-org/ghostty/discussions/7046

---

## Minor Pitfalls

---

### Pitfall 11: Avalonia Cursor Types Render Incorrectly on macOS

**What goes wrong:** Resize and directional cursors defined using Avalonia's `StandardCursorType` enum render differently on macOS than on Windows. Diagonal corner resize cursors display as tiny crosses instead of diagonal arrows. This affects the image annotation editor and region selection UI.

**Prevention:** Test all custom cursor types on macOS hardware during development. Use platform-conditional cursor assignments or native cursor images where Avalonia's cursors are visually wrong.

**Phase:** Phase 3 (annotation editor). Flag during UI review.

**Source:** https://github.com/AvaloniaUI/Avalonia/discussions/11925

---

### Pitfall 12: Avalonia NativeMenu Default Items Conflict with User Menus

**What goes wrong:** Avalonia's default macOS menu bar includes items (Edit menu, selectors) that conflict with user-defined `NativeMenu` entries. This can cause duplicate menu sections or broken ObjC selectors (Undo, Redo, Cut/Copy/Paste stop working).

**Prevention:**
- Use `NativeMenu` (not Avalonia's in-window `Menu` control) for the macOS menu bar — these are different things.
- Explicitly define all top-level menu structure via `NativeMenu` in XAML. Do not rely on Avalonia's auto-generated defaults.
- Test the full menu bar (including Edit, Window, Help menus) on a real macOS build, not just in the IDE.

**Phase:** Phase 1 (app shell / menu bar setup).

**Sources:**
- https://github.com/avaloniaui/avalonia/issues/19219
- https://docs.avaloniaui.net/docs/reference/controls/nativemenu

---

### Pitfall 13: GIF Capture Is CPU-Intensive Without Hardware Encoding

**What goes wrong:** GIF encoding is CPU-bound and has no hardware acceleration path on Apple Silicon. Encoding a GIF from ScreenCaptureKit frames in real-time at 60fps will peg a CPU core and produce large files with mediocre quality compared to modern video formats.

**Prevention:**
- Limit GIF capture to a maximum frame rate (10-15fps is standard for GIF). Make this configurable.
- Use a palette-quantization algorithm (e.g., octree or median-cut) to optimize color depth before encoding.
- Alternatively, capture to a temporary video file and convert to GIF post-capture using FFmpeg — this decouples capture from encoding and allows better quality optimization.
- Set explicit file size limits and warn users when GIF output will be large.

**Phase:** Phase 3 (GIF capture). Design the GIF pipeline around post-capture conversion rather than real-time encoding.

---

### Pitfall 14: Multi-Display Coordinate System Is Not Unified

**What goes wrong:** macOS uses a flipped Y-axis coordinate system for `CGRect` in some APIs (origin at bottom-left) and a top-left origin in others. When working with multiple displays, each display has its own `SCDisplay.frame` in screen coordinates, but window positions from `SCWindow.frame` use a unified virtual display space. Mixing coordinate systems when mapping captured regions to display positions produces off-by-one errors and misaligned overlays on secondary monitors.

**Prevention:**
- Always normalize coordinates to a single reference frame at the API boundary layer. Never pass raw `CGRect` values from one API into another without explicit coordinate conversion.
- Write a utility that converts between ScreenCaptureKit screen coordinates and Avalonia's window coordinate system. Test this against a secondary display positioned above, below, left, and right of the primary.
- Test the region selector on a multi-monitor setup from day one.

**Phase:** Phase 2 (region capture). Include multi-display as a required test case.

---

## Phase-Specific Warnings Summary

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Capture foundation (ScreenCaptureKit setup) | NSRunLoop not initialized — async calls hang | Initialize NSApplication before any ScreenCaptureKit call |
| Image manipulation (porting ShareX libs) | System.Drawing.Common crashes on macOS | Audit and replace all GDI+ usage with SkiaSharp/ImageSharp before first build |
| Distribution / signing pipeline | Notarization kills app without JIT entitlement | Add `cs.allow-jit` to entitlements.plist before first distribution build |
| Distribution / signing pipeline | Ad-hoc signing causes TCC to forget permissions | Establish stable Developer ID certificate from the start |
| Permission UX | macOS 15 monthly re-prompt has no API workaround | Design graceful permission request UI, educate users |
| Region selection UI | Avalonia has no click-through transparency | Use native NSWindow APIs via P/Invoke for overlay |
| Region selection UI | Multi-display coordinate mismatch | Build unified coordinate conversion utility; test multi-display early |
| Global hotkey system | macOS 15.4 regression breaks global hotkeys | Build in-app fallback for all hotkey actions |
| Video / GIF recording | SCStream drops frames silently | Implement error delegate, process frames on high-priority queue |
| Video / GIF recording | Audio capture fails on macOS 15 with error -3805 | Have AVAudioEngine fallback; test on macOS 14 and 15 separately |
| App Store / sandbox (if pursued) | Sandbox crashes Avalonia on M1 unless single-file | Skip sandbox for v1; distribute via direct download + notarization |
| Annotation editor | Avalonia trackpad drag lag | Test on hardware trackpad early; flag for native event handling if needed |
| Annotation editor | Cursor types render incorrectly on macOS | Test all cursors on macOS; use native cursor images as needed |
| Menu bar integration | NativeMenu default items conflict | Define full menu structure explicitly; test on real macOS build |
| GIF capture | CPU-intensive real-time encoding | Use post-capture FFmpeg conversion, cap frame rate at 10-15fps |

---

## Sources

- Apple ScreenCaptureKit Documentation: https://developer.apple.com/documentation/screencapturekit/
- dotnet/macios GetShareableContentAsync hang: https://github.com/dotnet/macios/issues/17350
- macOS Sequoia weekly permission prompts: https://9to5Mac.com/2024/08/06/macos-sequoia-screen-recording-privacy-prompt/
- macOS 15.1 reduced prompt frequency: https://www.idownloadblog.com/2024/10/09/macos-sequoia-15-1-macos-screen-recording-prompts-frequency-reduced/
- System.Drawing.Common Windows-only breaking change: https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only
- Avalonia macOS development guide: https://docs.avaloniaui.net/docs/guides/platforms/macos-development
- Avalonia macOS known issues discussion: https://github.com/AvaloniaUI/Avalonia/discussions/11925
- Avalonia pass-through transparency discussion: https://github.com/AvaloniaUI/Avalonia/discussions/11911
- Avalonia sandbox crash issue: https://github.com/AvaloniaUI/Avalonia/issues/9764
- Avalonia macOS tray icon adjustments: https://github.com/avaloniaui/avalonia/issues/19655
- Avalonia NativeMenu default items issue: https://github.com/avaloniaui/avalonia/issues/19219
- ScreenCaptureKit Sequoia XPC issue: https://developer.apple.com/forums/thread/760112
- TCC code signature stability: https://developer.apple.com/forums/thread/760483
- SCStream audio failure macOS 15: https://github.com/ronaldoussoren/pyobjc/issues/647
- ScreenCaptureKit architecture analysis: https://fatbobman.com/en/posts/screensage-from-pixel-to-meta/
- macOS 15+: ScreenCaptureKit ignores content protection flags: https://github.com/tauri-apps/tauri/issues/14200
- .NET notarization requirements: https://learn.microsoft.com/en-us/dotnet/core/install/macos-notarization-issues
- Notarizing .NET console apps for macOS: https://www.kenmuse.com/blog/notarizing-dotnet-console-apps-for-macos/
- macOS 15.4 global hotkey regression: https://github.com/ghostty-org/ghostty/discussions/7046
- ShareX macOS porting discussion: https://github.com/ShareX/ShareX/issues/5440
