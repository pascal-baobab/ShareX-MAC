---
phase: 02-core-capture
plan: 03
subsystem: ui
tags: [skia, avalonia, overlay, region-selector, window-picker, capture]

# Dependency graph
requires:
  - phase: 02-01
    provides: ICaptureService interface, CaptureResult, CGRectCapture, WindowInfo types
  - phase: 02-02
    provides: ScreenCaptureKitBridge implementation of ICaptureService
provides:
  - RegionSelectorWindow fullscreen overlay with SkiaSharp crosshair, selection rect, dimension label, magnifier
  - RegionSelectorViewModel with mouse tracking, drag geometry, freeze toggle, keyboard shortcuts
  - WindowPickerOverlay fullscreen overlay with window highlight and name label
  - WindowPickerViewModel with window list hit-testing, shadow toggle, capture invocation
affects: [02-04, 02-05, capture-workflow, hotkey-integration]

# Tech tracking
tech-stack:
  added: []
  patterns: [ICustomDrawOperation SkiaSharp rendering for overlay windows, fullscreen transparent borderless Window pattern]

key-files:
  created:
    - ShareXMac.App/Views/RegionSelectorWindow.axaml
    - ShareXMac.App/Views/RegionSelectorWindow.axaml.cs
    - ShareXMac.App/ViewModels/RegionSelectorViewModel.cs
    - ShareXMac.App/Views/WindowPickerOverlay.axaml
    - ShareXMac.App/Views/WindowPickerOverlay.axaml.cs
    - ShareXMac.App/ViewModels/WindowPickerViewModel.cs
  modified: []

key-decisions:
  - "SkiaSharp ICustomDrawOperation via ISkiaSharpApiLeaseFeature for all overlay rendering"
  - "Screen bitmap captured on overlay open for magnifier; replaced on freeze toggle"
  - "CGWindowListCopyWindowInfo uses top-left origin (same as Avalonia) so no coordinate conversion needed"

patterns-established:
  - "Fullscreen transparent overlay: SystemDecorations=None, TransparencyLevelHint=Transparent, Background=Transparent, Topmost=True, WindowState=FullScreen"
  - "SkiaSharp overlay rendering: ICustomDrawOperation with ISkiaSharpApiLeaseFeature.Lease() for SKCanvas access"
  - "Dark pill label: #CC000000 bg, white text, 12px SemiBold, 6px H / 2px V padding, 4px corner radius"

requirements-completed: [CAPT-01, CAPT-02, CAPT-05]

# Metrics
duration: 5min
completed: 2026-03-25
---

# Phase 02 Plan 03: Region Selector and Window Picker Overlays Summary

**SkiaSharp-rendered fullscreen overlays for region selection (crosshair, magnifier, dimension label, freeze) and window picking (highlight, name label, shadow toggle)**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-25T20:40:22Z
- **Completed:** 2026-03-25T20:45:03Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- RegionSelectorWindow renders crosshair (white with black shadow), selection rectangle (2px accent border, 10% accent fill), dimension label, and 5x magnifier loupe via SkiaSharp ICustomDrawOperation
- RegionSelectorViewModel supports Escape (cancel), Enter (confirm), Space (freeze toggle), Shift (square constraint), Ctrl (center anchor)
- WindowPickerOverlay highlights hovered windows with 30% accent blue overlay and shows floating window name/owner label
- WindowPickerViewModel fetches window list from GetWindowListAsync, hit-tests cursor against window bounds, invokes CaptureWindowAsync on click, Tab toggles shadow inclusion

## Task Commits

Each task was committed atomically:

1. **Task 1: Create RegionSelectorWindow with SkiaSharp overlay rendering and RegionSelectorViewModel** - `9bf0380ae` (feat)
2. **Task 2: Create WindowPickerOverlay with window highlight rendering and WindowPickerViewModel** - `7d729b728` (feat)

## Files Created/Modified
- `ShareXMac.App/Views/RegionSelectorWindow.axaml` - Fullscreen transparent borderless Avalonia window for region capture
- `ShareXMac.App/Views/RegionSelectorWindow.axaml.cs` - SkiaSharp ICustomDrawOperation rendering: screen dim, crosshair, selection rect, dimension label, magnifier loupe, freeze indicator
- `ShareXMac.App/ViewModels/RegionSelectorViewModel.cs` - Mouse tracking, drag geometry with Shift/Ctrl modifiers, freeze toggle, CaptureRegionAsync/CaptureFreezeAsync invocation
- `ShareXMac.App/Views/WindowPickerOverlay.axaml` - Fullscreen transparent borderless Avalonia window for window selection
- `ShareXMac.App/Views/WindowPickerOverlay.axaml.cs` - SkiaSharp ICustomDrawOperation rendering: screen dim, window highlight (30% accent), window name label, shadow toggle indicator
- `ShareXMac.App/ViewModels/WindowPickerViewModel.cs` - Window list from GetWindowListAsync, cursor hit-testing, CaptureWindowAsync on click, shadow toggle

## Decisions Made
- Used ISkiaSharpApiLeaseFeature (Avalonia.Skia namespace) to access SKCanvas from ICustomDrawOperation.Render(ImmediateDrawingContext) instead of deprecated ISkiaDrawingContextImpl
- Screen bitmap for magnifier is captured once via CaptureFullScreenAsync on overlay open, avoiding continuous capture overhead; replaced when freeze is toggled
- CGWindowListCopyWindowInfo returns top-left origin coordinates (matching Avalonia convention), so WindowPickerViewModel uses window bounds directly without Cocoa coordinate conversion
- Nearest-neighbor filtering (SKFilterQuality.None) for magnifier bitmap rendering to preserve pixel-perfect zoom appearance

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Known Stubs
None - all functionality is fully wired to ICaptureService methods.

## Next Phase Readiness
- Region selector and window picker overlays are ready to be integrated into the capture workflow
- Plan 02-04 (Capture Preview Popup) and Plan 02-05 (History View) can reference these overlays
- AppViewModel.CaptureScreenCommand needs to be updated to launch RegionSelectorWindow/WindowPickerOverlay (integration in a later plan)

## Self-Check: PASSED

All 7 files verified present. Both task commits (9bf0380ae, 7d729b728) confirmed in git log.

---
*Phase: 02-core-capture*
*Completed: 2026-03-25*
