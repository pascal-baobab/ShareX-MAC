# Project Research Summary

**Project:** ShareXMac — macOS screen capture and sharing app (ShareX port)
**Domain:** Native desktop utility app (C# / Avalonia UI / macOS Apple APIs)
**Researched:** 2026-03-25
**Confidence:** MEDIUM-HIGH

## Executive Summary

ShareXMac is a macOS port of the Windows-only ShareX screen capture tool, built on C# with Avalonia UI as the cross-platform XAML framework. The recommended approach is a three-project solution: a platform-agnostic Core library, a macOS-specific interop layer targeting `net9.0-macos`, and an Avalonia App project. This structure allows ShareX's existing business logic (uploaders, workflow engine, history) to be ported with minimal friction while isolating the high-risk Apple native API integration (ScreenCaptureKit, AVFoundation, SharpHook) into a bounded layer. Avalonia 11 is the only mature C# UI framework with real macOS support and is used by the closest reference project (XerahS).

The core value proposition — "capture anything, share it anywhere, in one keystroke" — requires four interlocking systems before the product is useful: ScreenCaptureKit-based capture, global hotkeys via SharpHook, an after-capture workflow engine, and at least one upload destination (Imgur). Everything else, including screen recording, GIF, OCR, and advanced uploaders, layers on top and can be deferred. The feature dependency chain is strict: capture and hotkeys must be working before any other feature can be integrated.

The single largest risk category is macOS system integration. Apple's recent OS changes create unavoidable friction: Sequoia (15+) re-prompts users for screen recording permission monthly, macOS 15.4 introduced a regression that breaks global hotkeys for some apps, and App Sandbox must be skipped entirely for v1 because it crashes Avalonia on Apple Silicon. The porting risk from System.Drawing/GDI+ is equally critical — ShareX's original codebase uses GDI+ pervasively, and every affected code path throws `PlatformNotSupportedException` at runtime on macOS without a compile-time warning. Both issues must be resolved in Phase 1 before any feature work begins.

---

## Key Findings

### Recommended Stack

The stack is built around Avalonia 11.3.12 on .NET 9.0, which is the only credible path for C# UI on macOS at this quality level. The native API surface is accessed through the `net9.0-macos` TFM (via `dotnet workload install macos`) which provides official C# bindings to ScreenCaptureKit, AVFoundation, and the Vision framework. For standalone Avalonia apps, the recommended pattern for high-risk Apple APIs is a thin native dylib (Swift/ObjC) exposing a C-callable interface that C# P/Invokes into — this isolates Apple API churn from the managed codebase.

Image processing uses SixLabors.ImageSharp (3.1.12) as the pure-managed replacement for ShareX's GDI+ calls. SkiaSharp (bundled with Avalonia) handles UI rendering and annotation canvas drawing. FFMpegCore (5.4.0) wraps a bundled FFmpeg binary for video encoding; ImageSharp's own GIF encoder handles animated GIF output without the binary dependency. Global hotkeys use SharpHook 7.1.1 (CGEventTap/libuiohook), ReactiveUI is the MVVM framework via Avalonia.ReactiveUI, and SQLite with sqlite-net-pcl manages capture history.

**Core technologies:**
- **Avalonia 11.3.12**: Cross-platform XAML UI — only mature C# framework with real macOS support; uses Skia/Metal rendering
- **.NET 9.0 / C# 13**: Runtime — best macOS ARM performance; migrate to .NET 10 LTS when available
- **net9.0-macos TFM (dotnet/macios)**: Apple API bindings — official path to ScreenCaptureKit, AVFoundation, Vision Framework
- **SixLabors.ImageSharp 3.1.12**: Image encode/decode/GIF — pure managed replacement for ShareX's System.Drawing usage
- **SkiaSharp (Avalonia-bundled)**: Rendering and annotation canvas — already in the dependency graph; zero added cost
- **SharpHook 7.1.1**: Global hotkeys — wraps CGEventTap; only credible cross-platform hotkey library for .NET
- **FFMpegCore 5.4.0**: Video encoding — bundled FFmpeg binary handles MP4 output from SCStream frames
- **Avalonia.ReactiveUI 11.3.12**: MVVM framework — Avalonia's default; pairs with SharpHook.Reactive for hotkey streams
- **SQLite + sqlite-net-pcl**: Capture history — standard embedded DB for desktop apps; well-documented with Avalonia
- **AWSSDK.S3 4.0.18.4**: S3/R2 uploads — AWS SDK v4; R2 uses S3-compatible endpoint
- **Parcel (Avalonia tool)**: Packaging — automates notarization, DMG, Info.plist; use over manual scripts

**Do not use:**
- `System.Drawing` / `System.Drawing.Common` — throws `PlatformNotSupportedException` on macOS at runtime
- .NET MAUI — Mac Catalyst wraps UIKit, not AppKit; inferior native feel for desktop use
- Avalonia `KeyBindings` for global hotkeys — only fires when app window is focused

### Expected Features

The product is competing against CleanShot X, Shottr, and Snagit on macOS. Missing any table-stakes feature means users reach for CleanShot X instead.

**Must have (table stakes):**
- Region, window, full-screen capture — product is nothing without these
- Global hotkeys — must work when app is hidden; required for any background capture utility
- Clipboard copy after capture — users expect this automatically
- Local file save with configurable path and filename template
- Basic annotation: arrows, text, rectangle, highlight, blur — minimum to justify switching from Cmd+Shift+4
- Capture history with thumbnails — reduces "where did my screenshot go?" frustration
- After-capture workflow engine — save + copy + upload chaining; ShareX's signature differentiator
- Menu bar presence with NativeMenu — macOS utility convention; required for quick access without main window
- Permission onboarding flow — smooth Screen Recording + Input Monitoring setup; app is useless without it

**Should have (competitive differentiators):**
- Screen recording to MP4 — expected from a "ShareX port"; high complexity; high value
- GIF capture — highly requested for demos and bug reports; depends on recording pipeline
- OCR (text from screen) — Vision framework makes it macOS-native and free; CleanShot X and Shottr both have it
- Imgur upload (anonymous + authenticated) — instant shareable link with zero config; highest-value upload feature
- S3 / Cloudflare R2 / GCS upload — developer and power user segment
- Custom HTTP uploader — ShareX's killer differentiator; configurable JSON endpoint schema
- Scrolling capture — no native macOS equivalent; large gap in the market
- Pin / float screenshot (always-on-top) — useful for reference while working; Shottr and CleanShot X both have it
- Freeze screen capture — captures transient UI (hover states, tooltips)
- Image beautifier (padding, background, shadow) — trendy for Twitter/LinkedIn sharing

**Defer (v2+):**
- Video editor (trim/cut/effects) — scope creep; trim-only is fine for v1
- Social media direct posting — API fragility, constant breakage; share link instead
- Browser extension — separate product surface; out of scope
- iOS/mobile companion — desktop-first; App Store complexity not worth it for v1
- Built-in cloud storage — requires backend infrastructure, billing, GDPR; integrate existing services only
- Full document OCR indexing — OCR-to-clipboard is the use case; searchable history is a separate product

### Architecture Approach

The system follows a strict four-layer architecture: Shell (Avalonia TrayIcon + NativeMenu + hotkeys), Workflow (TaskManager + AfterCaptureQueue), Feature Layers (Capture, Edit, Upload), and Native Interop (ScreenCaptureKit, SharpHook, AVFoundation). Layers only communicate downward. The Workflow layer is the critical join point — it is a direct port of ShareX's `TaskHelpers.cs` + `WorkflowConfig` pattern where the after-capture queue sequences every post-capture step. The Native Interop layer is the highest-risk component: Apple API bindings for ScreenCaptureKit work best through a thin native dylib (Swift/ObjC) exposing a C ABI, rather than direct ObjC runtime P/Invoke, which reduces Apple API churn exposure.

**Major components:**
1. **Native Interop Layer** — ScreenCaptureKit bridge, TCC permission manager, SharpHook, AVFoundation writer, NSPasteboard; highest risk; must be built first
2. **Capture Layer** — SCKitBridge, RegionSelector overlay, RecordingManager, GifEncoder; depends on interop layer
3. **Workflow Layer** — TaskManager, WorkflowConfig, AfterCaptureQueue; orchestrates capture → edit → upload pipeline
4. **Edit Layer** — ImageEditor (SkiaSharp annotation canvas ported from ShareX.ImageEditor), ImageEffects; can develop in parallel with Workflow
5. **Upload Layer** — UploadersLib (Imgur, S3, GCS, R2, custom), UploadQueue, ClipboardService, CaptureHistory; independently testable; build last
6. **Shell Layer** — Avalonia MainWindow, TrayIcon + NativeMenu, HotkeyService, PermissionsGate; wires everything together

**Key architecture decisions:**
- ScreenCaptureKit over CGWindowList — CGWindowListCreateImage is deprecated in macOS 15 (Sequoia)
- SCContentSharingPicker for window capture — system handles permission prompt; no custom window list UI needed
- Skip App Sandbox for v1 — distribute via direct download + notarization; sandbox crashes Avalonia on Apple Silicon
- Thin native dylib for Apple APIs — isolates ScreenCaptureKit/AVFoundation from C# codebase churn
- `Background="{x:Null}"` for transparent overlay — not `"Transparent"` — for correct click-through behavior

### Critical Pitfalls

1. **ScreenCaptureKit requires active NSRunLoop** — all async SCKit calls hang indefinitely without `NSApplication.Init()`. Initialize NSApplication before any capture call; never call SCKit from a `Task.Run` background thread without run loop setup. Blocks all capture work if wrong. (Phase 1)

2. **System.Drawing / GDI+ crashes on macOS at runtime** — ShareX's entire image pipeline uses `System.Drawing.Common`, which throws `PlatformNotSupportedException` on .NET 6+ macOS. Audit every ShareX library before porting; replace all usages with SkiaSharp/ImageSharp. This is not a compile-time error — it fails at runtime. (Phase 1, top porting blocker)

3. **App Sandbox breaks Avalonia + Screen Recording + Accessibility** — sandboxed Avalonia apps crash on M1/M2 unless published as single-file; global hotkeys silently stop working in sandbox. Skip the App Store for v1; distribute via Developer ID notarization only. (Phase 1 architecture decision)

4. **Transparent overlay window has no click-through in Avalonia** — region selection UI requires a fullscreen transparent window that passes events through in empty areas. Pure Avalonia cannot do this; requires P/Invoke to `NSWindow.ignoresMouseEvents` or a native ObjC helper dylib. Must be designed before Phase 2 implementation begins. (Phase 2)

5. **macOS 15.4 global hotkey regression** — hotkeys registered via Accessibility API stopped working globally in macOS 15.4 for some apps; they only fire in-foreground. Design all hotkey features with in-app menu fallbacks from the start. (Phase 2)

6. **Notarization requires JIT entitlement** — hardened runtime blocks JIT by default; .NET (non-AOT) apps need `com.apple.security.cs.allow-jit` in entitlements.plist or the app crashes immediately post-notarization. Set this up before the first distribution build. (Phase 1)

7. **macOS Sequoia monthly screen recording re-prompts** — no developer API exists to suppress recurring permission dialogs on macOS 15+. Design graceful permission UX with clear messaging; treat denied permission as recoverable state, not fatal error. (Phase 1 UX)

---

## Implications for Roadmap

Based on research, the feature dependency chain and pitfall clustering suggest 5 phases. The architecture's bottom-up build order (Native Interop → Capture → Workflow → Edit → Upload) is validated by the pitfall research: every critical pitfall (NSRunLoop, GDI+, sandbox, click-through, notarization) must be resolved in Phases 1-2 or it blocks all subsequent work.

### Phase 1: Foundation and App Shell

**Rationale:** All capture, hotkey, and feature work depends on a working macOS app shell with correct permissions, signing, and a confirmed native interop pattern. GDI+ audit and System.Drawing replacement must happen before any ShareX library is ported — this is the top porting blocker. Sandbox and signing decisions made here propagate through the entire architecture.

**Delivers:** Working Avalonia app with TrayIcon + NativeMenu, confirmed ScreenCaptureKit integration taking one screenshot, global hotkey fires a callback, permissions onboarding flow, Developer ID signing and notarization pipeline.

**Addresses:** Menu bar presence, macOS permissions onboarding, hotkey registration

**Avoids:** NSRunLoop hang (initialize NSApplication), System.Drawing crash (audit + replace before any port work), App Sandbox crash (skip sandbox, use notarization), JIT entitlement crash (add to entitlements.plist from day one), NativeMenu default item conflicts (define full menu structure explicitly), TCC identity instability (stable Developer ID from the start)

**Research flags:** NEEDS RESEARCH — thin native dylib pattern for SCKit (limited standalone .NET examples); TFM approach vs P/Invoke tradeoffs

---

### Phase 2: Core Capture

**Rationale:** Capture is the entire product foundation. Region, window, and full-screen capture plus their permission flows must be solid before the workflow engine has anything to process. The transparent overlay click-through problem must be solved here — it cannot be deferred without blocking region capture entirely.

**Delivers:** Region, window (via SCContentSharingPicker), and full-screen capture all working. RegionSelector overlay with correct click-through. Clipboard copy and local file save. Basic capture history.

**Addresses:** Region/area capture, window capture, full-screen capture, clipboard copy, local file save, capture history/recents, screenshot preview

**Avoids:** Transparent overlay click-through (P/Invoke to NSWindow APIs or native dylib), multi-display coordinate mismatch (build unified coordinate conversion utility; test multi-display from day one), SCStream frame drops (implement error delegate, high-priority dispatch queue)

**Research flags:** NEEDS RESEARCH — specific P/Invoke pattern for NSWindow click-through transparency in Avalonia 11; SCContentSharingPicker C# invocation pattern

---

### Phase 3: Workflow Engine and Hotkeys

**Rationale:** WorkflowConfig, TaskManager, and AfterCaptureQueue are the ShareX differentiator. They are also the glue layer connecting capture outputs to edit and upload destinations. HotkeyService connects SharpHook events to TaskManager, completing the background-capture loop.

**Delivers:** Configurable after-capture workflow (save + copy + upload chaining), per-hotkey workflow configuration, full HotkeyService wired to TaskManager, complete settings UI in MainWindow.

**Addresses:** After-capture workflow engine, per-hotkey workflow overrides, global hotkeys (full integration), main window settings panels

**Avoids:** macOS 15.4 hotkey regression (build in-app menu fallback for every hotkey action; surface hotkey status indicator in UI), one big MainWindow ViewModel (split into TrayMenuViewModel, HistoryViewModel, SettingsViewModel, CaptureWorkflowViewModel from day one)

**Research flags:** Standard patterns — ReactiveUI + Avalonia workflow is well-documented; ShareX TaskHelpers.cs is a direct port target

---

### Phase 4: Annotation Editor and Image Effects

**Rationale:** The annotation editor is the minimum viable differentiator that justifies switching from Cmd+Shift+4. It can be developed in parallel with Phase 3 because it takes a captured image as input and has no dependency on the workflow engine being complete.

**Delivers:** Annotation canvas with arrows, text, rectangle, highlight, blur/redact, and shapes. Image effects (watermark, resize, border). Pin/float screenshot window. Image beautifier (background, padding, shadow).

**Addresses:** Basic annotation, blur/redact, image beautifier, pin/float screenshot

**Avoids:** Avalonia trackpad drag lag (test on hardware trackpad early; consider native NSEvent handling for drag paths), incorrect cursor types on macOS (test all cursors on macOS hardware; use native cursor images as fallback), GDI+ in ShareX.ImageEditor (replace all with SkiaSharp)

**Research flags:** NEEDS RESEARCH — SkiaSharp `ICustomDrawOperation` annotation canvas performance at high resolution; ShareX.ImageEditor GDI+ migration surface area

---

### Phase 5: Upload Destinations and Sharing

**Rationale:** Upload integration is independently testable against a working capture pipeline. Imgur (anonymous) is the highest-value first upload destination — it requires no user account and delivers the "share anywhere in one keystroke" value prop. S3/R2/custom uploaders serve the power-user segment and directly differentiate from CleanShot X.

**Delivers:** Imgur upload (anonymous + OAuth2), S3/Cloudflare R2 upload, Google Cloud Storage upload, custom HTTP uploader (JSON config), full capture history with cloud links, URL shortening, clipboard URL copy on upload success.

**Addresses:** Imgur upload, S3/R2/GCS upload, custom uploader, URL shortening, QR code from URL

**Avoids:** Missing upload status in history (UploadQueue updates CaptureHistory status in real-time), credential storage (use macOS Keychain, not plain settings files)

**Research flags:** Standard patterns — AWSSDK.S3 v4 with R2 custom endpoint is documented; Imgur API v3 is stable

---

### Phase 6: Recording, GIF, and Advanced Capture (Post-MVP)

**Rationale:** Screen recording and GIF output share the SCStream + AVFoundation pipeline and should be built together. Scrolling capture is an independent high-complexity feature. All of Phase 6 is valuable but none of it blocks the MVP value proposition.

**Delivers:** Screen recording to MP4, GIF capture (post-capture FFmpeg conversion), scrolling capture (auto-scroll + stitch), OCR (Apple Vision Framework), freeze screen capture.

**Addresses:** Screen recording, GIF capture, scrolling capture, OCR, freeze screen

**Avoids:** SCStream audio capture failure on macOS 15 (implement AVAudioEngine fallback; test on both 14 and 15), GIF CPU intensity (post-capture FFmpeg conversion, not real-time; cap frame rate at 10-15fps), RecordingManager frame drops (error delegate + high-priority dispatch queue established in Phase 2)

**Research flags:** NEEDS RESEARCH — AVFoundation CMSampleBuffer write pipeline from C# (thin dylib pattern); scrolling capture accessibility API usage; Vision framework VNRecognizeTextRequest in standalone Avalonia app

---

### Phase Ordering Rationale

- **Native interop must come first** — ScreenCaptureKit NSRunLoop hang and GDI+ replacement are both hard blockers that appear at runtime, not compile time; discovering them late means rewriting already-built features
- **Capture before workflow** — the AfterCaptureQueue has nothing to queue without a working capture output; building the queue first produces untestable code
- **Edit in parallel with workflow** — the annotation editor takes a bitmap input and has no dependency on the workflow engine; parallelizing these saves calendar time
- **Upload last** — UploadersLib is the most independently testable component; it only needs an HTTP client and a file/image; no Apple API complexity
- **Recording/GIF deferred to Phase 6** — high complexity, separate AVFoundation pipeline, does not affect the core screenshot + share value proposition

---

### Research Flags

Phases likely needing deeper research during planning:

- **Phase 1:** Thin native dylib (Swift/ObjC) pattern for ScreenCaptureKit in a standalone Avalonia app — limited authoritative documentation exists for this exact setup; XerahS source is the best reference
- **Phase 2:** NSWindow click-through P/Invoke pattern in Avalonia 11 — community workarounds exist but no official Avalonia API; test against multiple macOS versions
- **Phase 2:** SCContentSharingPicker C# invocation — dotnet/macios bindings exist but real-world usage in Avalonia is undocumented
- **Phase 4:** SkiaSharp `ICustomDrawOperation` for annotation canvas performance — high-resolution annotation with multiple overlays needs validation
- **Phase 6:** AVFoundation CMSampleBuffer write pipeline from C# — complex; thin native dylib approach strongly recommended; validate before building RecordingManager
- **Phase 6:** Apple Vision Framework in standalone Avalonia app — bindings exist in dotnet/macios but practical Avalonia integration is not documented

Phases with standard patterns (research-phase likely unnecessary):

- **Phase 3:** WorkflowConfig + TaskManager — direct port of ShareX's TaskHelpers.cs; well-understood pattern; ReactiveUI documentation is thorough
- **Phase 5:** Imgur API v3 + AWSSDK.S3 for R2 — both are stable APIs with good .NET library support; Cloudflare R2 S3-compatible endpoint is documented

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | NuGet versions confirmed for all major packages; Avalonia 11.3.12, SharpHook 7.1.1, ImageSharp 3.1.12, AWSSDK 4.0.18.4 all verified Oct–Feb 2025/2026 |
| Features | HIGH | Based on direct competitive analysis of CleanShot X, Shottr, Snagit, Kap, and ShareX Windows; feature gaps well-documented |
| Architecture | MEDIUM-HIGH | Four-layer pattern is sound; Apple API layer (SCKit thin dylib, AVFoundation from C#) is MEDIUM — confirmed pattern but limited standalone .NET documentation |
| Pitfalls | HIGH | All critical pitfalls have official Apple/Microsoft sources or confirmed Avalonia GitHub issues; macOS 15.4 hotkey regression confirmed active |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **ScreenCaptureKit thin dylib pattern**: The recommended approach (C-callable Swift/ObjC dylib + P/Invoke) lacks a single authoritative C# guide. During Phase 1 planning, spike this pattern explicitly before committing to the full SCKit integration design. XerahS and `capture-screen-macos-csharp` on GitHub are the best starting references.

- **dotnet/macios TFM in standalone Avalonia**: The `net9.0-macos` TFM approach is documented for MAUI apps; using it in an Avalonia app alongside `net9.0` requires a multi-target project setup that is not officially documented in Avalonia's own docs. Validate the project structure in Phase 1 before porting any ShareX libraries.

- **macOS 15.4 hotkey regression status**: This was an active regression as of research date (March 2026). Monitor for Apple patch releases; the mitigation (in-app menu fallbacks) is the only viable defense until Apple fixes the underlying event tap behavior.

- **SCStream audio capture on macOS 15**: Apple bug `SCStreamErrorDomain Code=-3805` has no confirmed workaround. Phase 6 planning must include an AVAudioEngine fallback branch; audio capture should be treated as best-effort, not guaranteed.

- **Google.Apis.Storage latest version**: Research confirmed AWSSDK.S3 v4.0.18.4 and Imgur.API 5.0.0 but did not verify the latest Google.Apis.Storage.v1 version. Verify on NuGet before Phase 5.

---

## Sources

### Primary (HIGH confidence)
- [Avalonia NuGet Gallery v11.3.12](https://www.nuget.org/packages/avalonia) — version confirmation
- [Apple ScreenCaptureKit Developer Docs](https://developer.apple.com/documentation/ScreenCaptureKit/capturing-screen-content-in-macos) — capture API design
- [dotnet/macios — ScreenCaptureKit C# bindings](https://github.com/dotnet/macios/wiki/ScreenCaptureKit-macOS-xcode16.2-b2) — TFM binding surface
- [SharpHook NuGet v7.1.1](https://www.nuget.org/packages/SharpHook/) + [GitHub](https://github.com/TolikPylypchuk/SharpHook) — hotkey library
- [SixLabors.ImageSharp NuGet v3.1.12](https://www.nuget.org/packages/sixlabors.imagesharp/) — image processing
- [Microsoft: System.Drawing.Common Windows-only](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only) — GDI+ breaking change
- [Microsoft: macOS notarization requirements for .NET](https://learn.microsoft.com/en-us/dotnet/core/install/macos-notarization-issues) — JIT entitlement
- [AWSSDK.S3 NuGet v4.0.18.4](https://www.nuget.org/packages/AWSSDK.S3) — upload SDK
- [Avalonia macOS deployment guide](https://avaloniaui.net/blog/the-definitive-guide-to-building-and-deploying-avalonia-applications-for-macos) — Parcel, signing, notarization

### Secondary (MEDIUM confidence)
- [XerahS — ShareX Avalonia port reference](https://github.com/ShareX/XerahS) — architecture pattern validation
- [Avalonia pass-through transparency discussion #11911](https://github.com/AvaloniaUI/Avalonia/discussions/11911) — overlay click-through
- [Avalonia sandbox crash issue #9764](https://github.com/AvaloniaUI/Avalonia/issues/9764) — sandbox behavior on Apple Silicon
- [dotnet/macios GetShareableContentAsync hang #17350](https://github.com/dotnet/macios/issues/17350) — NSRunLoop pitfall
- [Recording to disk with ScreenCaptureKit — Nonstrict (2023)](https://nonstrict.eu/blog/2023/recording-to-disk-with-screencapturekit/) — AVFoundation pipeline
- [SCContentSharingPicker macOS Sequoia behavior — mjtsai (2024)](https://mjtsai.com/blog/2024/08/08/sequoia-screen-recording-prompts-and-the-persistent-content-capture-entitlement/) — permission UX
- [Avalonia NativeMenu default items issue #19219](https://github.com/avaloniaui/avalonia/issues/19219) — menu conflict
- [SCStream audio failure macOS 15](https://github.com/ronaldoussoren/pyobjc/issues/647) — audio capture bug

### Tertiary (LOW confidence / needs validation)
- [macOS 15.4 global hotkey regression — Ghostty #7046](https://github.com/ghostty-org/ghostty/discussions/7046) — active regression; status may change
- [ScreenCaptureKit architecture analysis — fatbobman](https://fatbobman.com/en/posts/screensage-from-pixel-to-meta/) — Swift-focused; C# applicability inferred

---

*Research completed: 2026-03-25*
*Ready for roadmap: yes*
