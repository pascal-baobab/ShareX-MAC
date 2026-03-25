---
phase: 02-core-capture
plan: 04
subsystem: ui
tags: [avalonia, reactiveui, capture-preview, history, observable-collection, auto-dismiss]

# Dependency graph
requires:
  - phase: 02-01
    provides: "CaptureResult, CaptureMode, ICaptureHistory, CaptureRecord, IOutputService contracts"
provides:
  - "CapturePreviewPopup: 280x80 floating window with auto-dismiss countdown and Copy/Save/Open actions"
  - "HistoryView: searchable, filterable 3-column thumbnail grid of past captures"
  - "HistoryItemCard: individual capture entry card with thumbnail, filename, date, mode badge"
  - "HistoryViewModel: ICaptureHistory binding with debounced search and mode filter"
  - "HistoryItemViewModel: CaptureRecord wrapper with computed display properties"
affects: [02-05-integration, 03-settings, 04-annotation]

# Tech tracking
tech-stack:
  added: []
  patterns: [HistoryItemViewModel wrapper for DB-to-UI binding, Observable.Interval countdown timer, static factory Show() for popup windows]

key-files:
  created:
    - ShareXMac.App/ViewModels/CapturePreviewViewModel.cs
    - ShareXMac.App/Views/CapturePreviewPopup.axaml
    - ShareXMac.App/Views/CapturePreviewPopup.axaml.cs
    - ShareXMac.App/ViewModels/HistoryViewModel.cs
    - ShareXMac.App/ViewModels/HistoryItemViewModel.cs
    - ShareXMac.App/Views/HistoryView.axaml
    - ShareXMac.App/Views/HistoryView.axaml.cs
    - ShareXMac.App/Views/HistoryItemCard.axaml
    - ShareXMac.App/Views/HistoryItemCard.axaml.cs
  modified: []

key-decisions:
  - "HistoryItemViewModel wrapper instead of extending CaptureRecord -- keeps DB model clean, provides computed ThumbnailBitmap/DisplayFilename/DisplayDate"
  - "ProgressBar control instead of manual Border width binding -- simpler, no custom converter needed"
  - "Client-side filename search with server-side mode filter -- sqlite-net QueryAsync handles mode; LIKE on FilePath deferred to avoid sqlite-net complexity"

patterns-established:
  - "HistoryItemViewModel wrapper: DB record -> lightweight VM with Bitmap/display properties for AXAML binding"
  - "Static Show() factory: creates VM + Window + wires events in one call for popup windows"
  - "Observable.Interval countdown: 50ms ticks for smooth progress bar animation with IsPaused pause support"

requirements-completed: [CAPT-06, OUT-04, OUT-05]

# Metrics
duration: 4min
completed: 2026-03-25
---

# Phase 02 Plan 04: Preview Popup and History UI Summary

**Floating 280x80 capture preview popup with 5s auto-dismiss and hover-pause, plus searchable 3-column history grid with mode filter chips and empty state**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-25T20:40:38Z
- **Completed:** 2026-03-25T20:44:48Z
- **Tasks:** 2
- **Files created:** 9

## Accomplishments
- CapturePreviewPopup: 280x80 borderless floating window positioned bottom-right with thumbnail, 3 action buttons, and a depleting progress bar countdown
- HistoryView: UserControl with search bar (300ms debounce), mode filter ToggleButtons (All/Region/Window/FullScreen/Freeze), 3-column UniformGrid of HistoryItemCards, and empty state
- HistoryViewModel loads from ICaptureHistory.QueryAsync with mode filter server-side and filename search client-side

## Task Commits

Each task was committed atomically:

1. **Task 1: Create CapturePreviewPopup with auto-dismiss countdown and action buttons** - `71f0c10ec` (feat)
2. **Task 2: Create HistoryView, HistoryItemCard, and HistoryViewModel with search and filter** - `fe26e2883` (feat)

## Files Created
- `ShareXMac.App/ViewModels/CapturePreviewViewModel.cs` - 5s countdown timer, Copy/Save/Open commands, hover-pause, DismissRequested event
- `ShareXMac.App/Views/CapturePreviewPopup.axaml` - 280x80 borderless window with thumbnail, buttons, progress bar
- `ShareXMac.App/Views/CapturePreviewPopup.axaml.cs` - Bottom-right positioning, pointer hover pause, Escape dismiss, static Show() factory
- `ShareXMac.App/ViewModels/HistoryViewModel.cs` - ICaptureHistory binding, debounced search, mode filter, delete command
- `ShareXMac.App/ViewModels/HistoryItemViewModel.cs` - CaptureRecord wrapper with ThumbnailBitmap, DisplayFilename, DisplayDate
- `ShareXMac.App/Views/HistoryView.axaml` - Search, filter chips, 3-column UniformGrid, empty state
- `ShareXMac.App/Views/HistoryView.axaml.cs` - UserControl code-behind
- `ShareXMac.App/Views/HistoryItemCard.axaml` - 100x100 thumbnail, filename, date, mode badge, context menu
- `ShareXMac.App/Views/HistoryItemCard.axaml.cs` - UserControl code-behind

## Decisions Made
- Used HistoryItemViewModel wrapper instead of extending CaptureRecord (keeps DB model clean)
- Used Avalonia ProgressBar instead of manual Border width binding (simpler, no converter needed)
- Client-side filename search because sqlite-net QueryAsync mode filter is server-side; LIKE on FilePath deferred to avoid ORM complexity

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added HistoryItemViewModel wrapper class**
- **Found during:** Task 2 (History UI)
- **Issue:** Plan mentioned the need for a wrapper but did not list it as a separate file. CaptureRecord lacks ThumbnailBitmap, DisplayFilename, DisplayDate computed properties needed for AXAML binding.
- **Fix:** Created HistoryItemViewModel.cs wrapping CaptureRecord with computed properties. Updated HistoryViewModel to use ObservableCollection<HistoryItemViewModel> instead of ObservableCollection<CaptureRecord>.
- **Files modified:** ShareXMac.App/ViewModels/HistoryItemViewModel.cs (new), ShareXMac.App/ViewModels/HistoryViewModel.cs
- **Verification:** HistoryItemCard.axaml bindings (ThumbnailBitmap, DisplayFilename, DisplayDate) match HistoryItemViewModel properties
- **Committed in:** fe26e2883

---

**Total deviations:** 1 auto-fixed (1 missing critical functionality)
**Impact on plan:** Necessary for correctness -- AXAML bindings require computed properties that CaptureRecord does not provide. No scope creep.

## Known Stubs

| File | Line | Stub | Resolution |
|------|------|------|------------|
| CapturePreviewViewModel.cs | 83-85 | CopyCommand body is no-op | Plan 02-05 wires through IOutputService |
| CapturePreviewViewModel.cs | 97 | OpenCommand opens Preview.app instead of annotation editor | Phase 4 (annotation) replaces this |
| HistoryViewModel.cs | 101 | CopyAsync body is no-op | Plan 02-05 wires through IOutputService |

All stubs are intentional and documented in the plan. They do not block the plan's goal -- the UI surfaces exist and the commands are valid ReactiveCommand targets that the integration plan (02-05) will complete.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- CapturePreviewPopup.Show() is ready to be called from the capture pipeline after each capture
- HistoryView is ready to be embedded in MainWindow content area (needs sidebar navigation wiring)
- Both will be connected to the capture pipeline and output service in plan 02-05 (integration)
- HistoryView filter chip selection needs wiring to SelectedModeFilter (code-behind or command) in integration

## Self-Check: PASSED

All 10 files verified present. Both task commits (71f0c10ec, fe26e2883) verified in git log.

---
*Phase: 02-core-capture*
*Completed: 2026-03-25*
