# Phase 1: Foundation and App Shell - Context

**Gathered:** 2026-03-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver a notarized Avalonia macOS app that runs from the menu bar, walks users through Screen Recording and Accessibility permissions on first launch, registers a global hotkey that fires while the app is in background, and takes one confirmed screenshot via ScreenCaptureKit. This phase establishes the entire native interop foundation, project structure, signing pipeline, and GDI+ replacement strategy that all subsequent phases depend on.

</domain>

<decisions>
## Implementation Decisions

### Native Interop Strategy
- Use a thin native dylib (Swift wrapper with C ABI) for ScreenCaptureKit access, P/Invoked from C# — isolates Apple API churn from the managed codebase
- Three-project structure: ShareXMac.Core (net9.0), ShareXMac.macOS (net9.0-macos interop layer), ShareXMac.App (Avalonia UI)
- Create IImageProcessor abstraction layer backed by ImageSharp/SkiaSharp early — replace all System.Drawing before porting any ShareX library
- net9.0 for Core/App projects, net9.0-macos for the macOS interop project

### App Distribution & Signing
- Developer ID notarization for direct download distribution — skip App Store and sandbox (sandbox crashes Avalonia on Apple Silicon)
- Use Parcel (Avalonia packaging tool) for automated notarization, DMG creation, and Info.plist management
- Include entitlements from day one: com.apple.security.cs.allow-jit and com.apple.security.cs.disable-library-validation in entitlements.plist

### Menu Bar & Permissions UX
- NativeMenu dropdown from TrayIcon with all capture actions — TrayIcon.Clicked doesn't fire on macOS so menu-only interaction is required
- Permission denied is a recoverable state — show banner in app, deep-link to System Settings, re-check on window focus; never fatal
- Settings window is a standalone window opened from the menu bar "Settings..." item — standard macOS pattern
- First-run experience is a step-by-step permission wizard guiding Screen Recording + Input Monitoring setup

### Claude's Discretion
- Internal code organization within the three-project structure
- Specific Swift/ObjC dylib API surface design
- CI/CD pipeline details (if any)
- Unit test framework selection

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- No existing code — greenfield project
- ShareX upstream libraries available for reference: ScreenCaptureLib, ImageEditor, UploadersLib, HistoryLib, MediaLib (all C#/GDI+, need adaptation)

### Established Patterns
- No established patterns yet — this phase defines them
- Research recommends: Avalonia.ReactiveUI for MVVM, SharpHook 7.1.1 for global hotkeys, SQLite for history

### Integration Points
- ScreenCaptureKit via thin native dylib → C# P/Invoke
- SharpHook → CGEventTap (requires Input Monitoring permission)
- Avalonia TrayIcon + NativeMenu → macOS menu bar
- NSApplication.Init() must be called before any ScreenCaptureKit usage (prevents NSRunLoop hang)

</code_context>

<specifics>
## Specific Ideas

- Reference XerahS (Avalonia ShareX port) for project structure patterns
- NSApplication.Init() must be the first call in the app — before any Task.Run or async capture code
- JIT entitlement (com.apple.security.cs.allow-jit) must be in entitlements.plist from the first build or notarized app crashes immediately
- Test on Apple Silicon (M1/M2) from day one — sandbox and JIT issues manifest differently on ARM vs Intel

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>
