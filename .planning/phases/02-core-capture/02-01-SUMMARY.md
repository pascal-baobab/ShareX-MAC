---
phase: 02-core-capture
plan: 01
subsystem: capture, output, history
tags: [capture-contracts, nspasteboard, sqlite, imagesharp, pinvoke, clipboard]

# Dependency graph
requires:
  - phase: 01-foundation-and-app-shell
    provides: "ICaptureService skeleton, ScreenCaptureKitBridge CGWindowList P/Invoke, project structure"
provides:
  - "CaptureMode enum (FullScreen, AllScreens, Region, Window, Freeze)"
  - "CaptureResult sealed record (PngBytes, Mode, CapturedAt, Width, Height, DisplayId)"
  - "Expanded ICaptureService with 7 methods covering all capture modes"
  - "CGRectCapture and WindowInfo types for region/window capture"
  - "IOutputService + OutputService (NSPasteboard clipboard + file save with token templates)"
  - "OutputOptions with configurable save directory and filename template"
  - "ICaptureHistory + SqliteCaptureHistory (sqlite-net-pcl at ~/Library/Application Support/ShareXMac/history.db)"
  - "CaptureRecord ORM model with inline JPEG thumbnails"
affects: [02-02-capture-modes, 02-03-region-selector, 02-04-preview-history, 02-05-freeze-capture]

# Tech tracking
tech-stack:
  added: [sqlite-net-pcl (macOS project)]
  patterns: [NSPasteboard P/Invoke clipboard write, token-based filename templates, inline JPEG thumbnails in SQLite]

key-files:
  created:
    - ShareXMac.Core/Capture/CaptureMode.cs
    - ShareXMac.Core/Capture/CaptureResult.cs
    - ShareXMac.Core/Output/IOutputService.cs
    - ShareXMac.Core/Output/OutputOptions.cs
    - ShareXMac.Core/History/ICaptureHistory.cs
    - ShareXMac.Core/History/CaptureRecord.cs
    - ShareXMac.macOS/Output/OutputService.cs
    - ShareXMac.macOS/History/SqliteCaptureHistory.cs
  modified:
    - ShareXMac.Core/Capture/ICaptureService.cs
    - ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs
    - ShareXMac.macOS/ShareXMac.macOS.csproj
    - .gitignore

key-decisions:
  - "NSPasteboard clipboard via raw P/Invoke (objc_msgSend pattern) not Process.Start pbcopy"
  - "Inline JPEG thumbnails (100x100 max, 80% quality) stored directly in SQLite rows"
  - "Token-based filename template with {date}, {time}, {type}, {counter} replacement"
  - "ScreenCaptureKitBridge stubs for new interface methods (to be implemented in 02-02)"

patterns-established:
  - "NSPasteboard P/Invoke pattern: ObjcGetClass -> SelRegisterName -> ObjcMsgSend for Objective-C runtime calls"
  - "CaptureResult as universal output type: all capture methods return this sealed record"
  - "Output pipeline order: clipboard -> file save -> history insert"
  - "SQLite lazy initialization with GetDbAsync pattern"

requirements-completed: [CAPT-01, CAPT-02, CAPT-03, CAPT-04, OUT-01, OUT-02, OUT-03, OUT-04, OUT-05]

# Metrics
duration: 10min
completed: 2026-03-25
---

# Phase 2 Plan 1: Capture Contracts & Output Pipeline Summary

**Core capture contracts (CaptureMode/CaptureResult/ICaptureService) with NSPasteboard clipboard write, token-based file save, and SQLite capture history with inline JPEG thumbnails**

## Performance

- **Duration:** 10 min
- **Started:** 2026-03-25T20:17:33Z
- **Completed:** 2026-03-25T20:27:25Z
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments
- Defined all Core capture contracts: CaptureMode enum, CaptureResult record, expanded ICaptureService with 7 methods covering all capture modes
- Implemented OutputService with NSPasteboard P/Invoke clipboard write (public.png UTI) and file save with {date}/{time}/{type}/{counter} token replacement
- Implemented SqliteCaptureHistory with sqlite-net-pcl, inline JPEG thumbnail generation via ImageSharp, and date/mode-filtered queries
- Zero System.Drawing references in any new code

## Task Commits

Each task was committed atomically:

1. **Task 1: Define Core capture contracts** - `f5e68f5da` (feat)
2. **Task 2: Implement OutputService and SqliteCaptureHistory** - `85179bf08` (feat)

## Files Created/Modified
- `ShareXMac.Core/Capture/CaptureMode.cs` - Enum: FullScreen, AllScreens, Region, Window, Freeze
- `ShareXMac.Core/Capture/CaptureResult.cs` - Sealed record with PngBytes, Mode, CapturedAt, Width, Height, DisplayId
- `ShareXMac.Core/Capture/ICaptureService.cs` - Expanded interface with 7 capture methods + helper types (CGRectCapture, WindowInfo)
- `ShareXMac.Core/Output/OutputOptions.cs` - Configuration record with save directory, filename template, clipboard toggle
- `ShareXMac.Core/Output/IOutputService.cs` - Interface with ProcessCaptureAsync returning saved file path
- `ShareXMac.Core/History/CaptureRecord.cs` - SQLite ORM model with [Table], [PrimaryKey], [AutoIncrement], ThumbnailBytes
- `ShareXMac.Core/History/ICaptureHistory.cs` - Interface with InsertAsync, QueryAsync, DeleteAsync, CountAsync
- `ShareXMac.macOS/Output/OutputService.cs` - NSPasteboard clipboard + file save + history insert pipeline
- `ShareXMac.macOS/History/SqliteCaptureHistory.cs` - sqlite-net-pcl implementation at ~/Library/Application Support/ShareXMac/history.db
- `ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs` - Updated to implement new ICaptureService (CaptureFullScreenAsync + stubs for 02-02)
- `ShareXMac.macOS/ShareXMac.macOS.csproj` - Added sqlite-net-pcl PackageReference
- `.gitignore` - Fixed Output/ rule to /Output/ to not block ShareXMac.Core/Output/

## Decisions Made
- NSPasteboard clipboard via raw P/Invoke (objc_msgSend pattern) -- matches CONTEXT.md decision, no subprocess calls
- Inline JPEG thumbnails (100x100, 80% quality) in SQLite -- avoids extra disk seeks in history list display
- Token-based filename with {date}, {time}, {type}, {counter} -- simple, no full template engine per CONTEXT.md
- ScreenCaptureKitBridge stubs for new methods -- keeps project compilable, real implementations in Plan 02-02

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Updated ScreenCaptureKitBridge to implement expanded ICaptureService**
- **Found during:** Task 1 (after replacing ICaptureService.cs)
- **Issue:** ScreenCaptureKitBridge implements ICaptureService but only had the old TakeSingleScreenshotAsync method. The expanded interface would break compilation.
- **Fix:** Converted TakeSingleScreenshotAsync logic to CaptureFullScreenAsync (returning CaptureResult), added NotImplementedException stubs for 5 other methods to be implemented in Plan 02-02.
- **Files modified:** ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs
- **Verification:** All interface methods present, file compiles with new contract
- **Committed in:** f5e68f5da (Task 1 commit)

**2. [Rule 3 - Blocking] Fixed .gitignore Output/ rule blocking Core Output directory**
- **Found during:** Task 1 (git add failed for ShareXMac.Core/Output/ files)
- **Issue:** The existing `Output/` gitignore rule (intended for ShareX build output) was catching `ShareXMac.Core/Output/` subdirectory
- **Fix:** Changed `Output/` to `/Output/` to only match the root-level Output directory
- **Files modified:** .gitignore
- **Verification:** git add succeeds for ShareXMac.Core/Output/ files
- **Committed in:** f5e68f5da (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (2 blocking)
**Impact on plan:** Both auto-fixes necessary for project compilability and git operations. No scope creep.

## Known Stubs

| File | Line | Stub | Reason | Resolved By |
|------|------|------|--------|-------------|
| ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs | ~108-127 | CaptureAllScreensAsync, CaptureRegionAsync, CaptureWindowAsync, CaptureFreezeAsync, GetWindowListAsync, GetActiveDisplayIds throw NotImplementedException | Interface expanded in this plan, implementations planned for 02-02 | Plan 02-02 |

## Issues Encountered
None beyond the deviations documented above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All Core contracts are stable and ready for downstream plans (02-02 through 02-05)
- OutputService and SqliteCaptureHistory are complete and can be wired into the capture pipeline immediately
- Plan 02-02 will implement the remaining capture modes in ScreenCaptureKitBridge using these contracts
- CaptureResult is the universal output type all capture paths will produce

## Self-Check: PASSED

All 10 created files verified present on disk. Both task commits (f5e68f5da, 85179bf08) verified in git log.

---
*Phase: 02-core-capture*
*Completed: 2026-03-25*
