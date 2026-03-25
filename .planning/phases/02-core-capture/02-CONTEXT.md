# Phase 2: Core Capture - Context

**Gathered:** 2026-03-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver the complete screenshot capture pipeline: region, window, and full-screen capture with crosshair overlay, clipboard copy, local file save with configurable templates, capture history with SQLite storage, floating preview thumbnail, and freeze-screen capture for transient UI. After this phase, users can capture any part of their screen and the result lands in clipboard, a local file, and searchable history.

</domain>

<decisions>
## Implementation Decisions

### Region Selection UI
- Fullscreen transparent Avalonia window with P/Invoke to NSWindow.ignoresMouseEvents for click-through in empty areas
- Custom SkiaSharp rendering for crosshair cursor, dimension labels, and selection rectangle on the overlay canvas
- Window capture uses CGWindowListCreateImage with specific windowID — enumerate windows via CGWindowListCopyWindowInfo P/Invoke, user clicks to select target window
- Multi-monitor support via CGGetActiveDisplayList P/Invoke — create one overlay per display with unified coordinate system

### Output Pipeline
- NSPasteboard via P/Invoke — write PNG data directly to macOS clipboard (no Process.Start("pbcopy"))
- Simple token replacement for filename templates: {date}, {time}, {type}, {counter} — no full template engine
- Default save location: ~/Pictures/ShareXMac/ — standard macOS convention
- PNG format by default with optional JPEG — ImageSharp handles both encoders

### History & Preview
- SQLite via sqlite-net-pcl for capture history storage (package already in NuGet from Phase 1)
- Floating overlay window in bottom-right after capture — auto-dismiss after 5 seconds, click opens editor/details
- History UI as a tab in MainWindow — thumbnail list view, searchable by date/type, filterable
- Freeze screen capture: CGWindowListCreateImage of entire screen stored as bitmap, displayed in fullscreen overlay for region selection over frozen image

### Claude's Discretion
- SQLite schema design for capture history table
- Exact overlay animation/transition behavior
- Thumbnail generation sizing and caching strategy
- Region selector keyboard shortcuts (Escape to cancel, Enter to confirm)
- Window shadow handling (with/without shadow toggle)

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs — CGWindowListCreateImage P/Invoke already working for full-screen capture
- ShareXMac.Core/Capture/ICaptureService.cs — interface with TakeSingleScreenshotAsync
- ShareXMac.Core/Capture/CaptureException.cs — exception type
- ShareXMac.macOS/Permissions/TccPermissionManager.cs — permission checks already working
- ShareXMac.App/ViewModels/AppViewModel.cs — TrayIcon command routing
- ShareXMac.App/Services/HotkeyService.cs — SharpHook global hotkey detection

### Established Patterns
- All projects target net9.0 (no Xcode dependency)
- Raw P/Invoke to macOS frameworks (CoreGraphics, CoreFoundation, ApplicationServices)
- ImageSharp Bgra32 for pixel data conversion from CGImage
- ReactiveUI MVVM pattern with ReactiveObject/ReactiveCommand
- Avalonia TrayIcon + NativeMenu for menu bar integration

### Integration Points
- ICaptureService needs expansion: region capture, window capture, freeze capture modes
- AppViewModel.CaptureScreenCommand needs to trigger region selector overlay
- HotkeyService needs to invoke capture modes (not just log)
- MainWindow needs History tab
- NativeMenu needs capture mode submenu items

</code_context>

<specifics>
## Specific Ideas

- CGWindowListCopyWindowInfo returns kCGWindowListOptionOnScreenOnly + kCGWindowListExcludeDesktopElements for clean window enumeration
- NSPasteboard.generalPasteboard → clearContents → setData:forType: with "public.png" UTI for clipboard
- Window shadow toggle: CGWindowListCreateImage with kCGWindowImageBoundsIgnoreFraming vs kCGWindowImageDefault
- Region selector should show live magnifier at cursor for pixel-perfect selection
- Preview thumbnail should show capture type icon (region/window/fullscreen) in corner

</specifics>

<deferred>
## Deferred Ideas

- Scrolling capture — deferred to Phase 6 (high complexity, separate pipeline)
- OCR from capture — deferred to Phase 6
- Annotation integration — deferred to Phase 4 (preview click → open editor)

</deferred>
