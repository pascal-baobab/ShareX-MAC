---
phase: 02-core-capture
plan: 02
subsystem: capture
tags: [p/invoke, coreg graphics, cgwindowlist, screen-capture, macos-native, imagesharp]

# Dependency graph
requires:
  - phase: 02-01
    provides: "ICaptureService interface, CaptureResult, CaptureMode, CGRectCapture, WindowInfo types"
provides:
  - "Full ICaptureService implementation in ScreenCaptureKitBridge (all 7 methods)"
  - "CaptureFullScreenAsync, CaptureAllScreensAsync, CaptureRegionAsync, CaptureWindowAsync, CaptureFreezeAsync"
  - "GetWindowListAsync with CGWindowListCopyWindowInfo CFArray/CFDictionary parsing"
  - "GetActiveDisplayIds via CGGetActiveDisplayList P/Invoke"
  - "Shared CaptureImageToResult helper pipeline (CGImage -> BGRA32 -> ImageSharp -> PNG)"
  - "Avalonia top-left to Cocoa bottom-left coordinate conversion for region capture"
affects: [02-03-region-overlay, 02-04-window-picker, 02-05-freeze-capture]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "CaptureImageToResult shared helper: CGWindowListCreateImage -> CGImageGetWidth/Height -> CGDataProviderCopyData -> BGRA32 -> ImageSharp PNG"
    - "CFDictionary value extraction helpers (GetIntFromDict, GetDoubleFromDict, GetStringFromDict)"
    - "Lazy<IntPtr> cached CFString keys for repeated dictionary lookups"
    - "CGDisplayBounds for multi-monitor display targeting"
    - "Coordinate conversion: primaryDisplayHeight - rect.Y - rect.Height"

key-files:
  created: []
  modified:
    - "ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs"

key-decisions:
  - "Extracted CaptureImageToResult helper to avoid code duplication across 5 capture methods"
  - "CaptureWindowAsync implemented alongside Task 1 since helper method was already ready"
  - "Lazy<IntPtr> for CFString key caching to avoid re-creating CFStrings on every GetWindowListAsync call"
  - "GetStringFromDict uses CFStringGetCStringPtr fast path with CFStringGetCString fallback"

patterns-established:
  - "CaptureImageToResult: all capture methods delegate to shared CGImage-to-PNG pipeline"
  - "CFDictionary extraction: GetIntFromDict/GetDoubleFromDict/GetStringFromDict for any future CoreFoundation dictionary work"
  - "GetDisplayBounds(displayId=0) auto-resolves to primary display"

requirements-completed: [CAPT-01, CAPT-02, CAPT-03, CAPT-04]

# Metrics
duration: 4min
completed: 2026-03-25
---

# Phase 02 Plan 02: Screen Capture Bridge Summary

**Full ICaptureService implementation with 7 methods (fullscreen, allscreens, region, window, freeze, window-list, display-ids) via CGWindowListCreateImage and CGWindowListCopyWindowInfo P/Invoke**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-25T20:32:25Z
- **Completed:** 2026-03-25T20:36:27Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Replaced Phase 1 proof-of-concept fullscreen-only capture with full ICaptureService implementation
- All 7 interface methods implemented: CaptureFullScreenAsync, CaptureAllScreensAsync, CaptureRegionAsync, CaptureWindowAsync, CaptureFreezeAsync, GetWindowListAsync, GetActiveDisplayIds
- Shared CaptureImageToResult pipeline eliminates duplication across 5 capture methods
- GetWindowListAsync parses CGWindowListCopyWindowInfo CFArray/CFDictionary into WindowInfo records with proper CFRelease cleanup
- Coordinate conversion from Avalonia top-left to Cocoa bottom-left in CaptureRegionAsync
- Window shadow toggle via kCGWindowImageBoundsIgnoreFraming in CaptureWindowAsync

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement FullScreen, Region, Freeze, AllScreens capture + GetActiveDisplayIds** - `810df4fef` (feat)
2. **Task 2: Implement CaptureWindowAsync and GetWindowListAsync with CGWindowListCopyWindowInfo** - `473818471` (feat)

## Files Created/Modified
- `ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs` - Full ICaptureService implementation with CGWindowListCreateImage, CGWindowListCopyWindowInfo, CGGetActiveDisplayList, CGDisplayBounds P/Invoke and ImageSharp BGRA32-to-PNG conversion

## Decisions Made
- Extracted CaptureImageToResult shared helper rather than duplicating CGImage->PNG pipeline in each method
- Used Lazy<IntPtr> for cached CFString keys to avoid per-call CFStringCreateWithCString overhead
- GetStringFromDict uses CFStringGetCStringPtr fast path (no allocation) with CFStringGetCString fallback
- Implemented CaptureWindowAsync in Task 1 commit since the helper was already available (mentioned in Task 2 acceptance criteria but logically belonged with Task 1's helper)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] CaptureWindowAsync implemented early (Task 1 instead of Task 2)**
- **Found during:** Task 1 (CaptureImageToResult helper creation)
- **Issue:** CaptureWindowAsync was planned for Task 2 but depends only on the CaptureImageToResult helper from Task 1
- **Fix:** Implemented in Task 1 alongside the other capture methods since the helper was ready
- **Files modified:** ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs
- **Verification:** Build succeeds, all acceptance criteria for both tasks met
- **Committed in:** 810df4fef (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical)
**Impact on plan:** Minor task ordering adjustment. All acceptance criteria met. No scope creep.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All capture methods are ready for UI integration (region selector, window picker, freeze overlay)
- Plans 02-03 through 02-05 can call any ICaptureService method
- GetWindowListAsync provides window enumeration for window picker UI
- GetActiveDisplayIds provides multi-monitor support for overlay creation

## Self-Check: PASSED

- FOUND: ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs
- FOUND: .planning/phases/02-core-capture/02-02-SUMMARY.md
- FOUND: commit 810df4fef (Task 1)
- FOUND: commit 473818471 (Task 2)

---
*Phase: 02-core-capture*
*Completed: 2026-03-25*
