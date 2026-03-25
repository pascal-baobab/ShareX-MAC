---
phase: 02-core-capture
plan: 05
subsystem: integration
tags: [avalonia, reactive-ui, tray-icon, hotkeys, sharphook, capture-pipeline, history-view]

# Dependency graph
requires:
  - phase: 02-01
    provides: "Core contracts (ICaptureService, IOutputService, ICaptureHistory), OutputService, SqliteCaptureHistory"
  - phase: 02-02
    provides: "ScreenCaptureKitBridge implementation of ICaptureService"
  - phase: 02-03
    provides: "RegionSelectorWindow, RegionSelectorViewModel, WindowPickerOverlay, WindowPickerViewModel"
  - phase: 02-04
    provides: "CapturePreviewPopup, HistoryView, HistoryViewModel, HistoryItemCard"
provides:
  - "Complete capture-to-output pipeline wired through AppViewModel"
  - "TrayIcon NativeMenu with 4 capture mode items"
  - "HotkeyService wired to capture commands (Cmd+Shift+3/4/5)"
  - "AppServices record for service container"
  - "MainWindow with embedded HistoryView"
affects: [03-annotation, 04-uploaders, 05-settings]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AppServices record as poor-man's DI container"
    - "TaskCompletionSource bridging overlay event callbacks to async/await"
    - "ProcessCaptureResult as common post-capture pipeline (OutputService then CapturePreviewPopup)"
    - "Dispatcher.UIThread.Post for cross-thread capture command invocation"

key-files:
  created: []
  modified:
    - ShareXMac.App/ViewModels/AppViewModel.cs
    - ShareXMac.App/App.axaml
    - ShareXMac.App/App.axaml.cs
    - ShareXMac.App/ViewModels/MainWindowViewModel.cs
    - ShareXMac.App/Views/MainWindow.axaml
    - ShareXMac.App/Views/MainWindow.axaml.cs
    - ShareXMac.App/Services/HotkeyService.cs

key-decisions:
  - "AppServices sealed record for service container -- no DI framework at this scale"
  - "Removed XAML-based Application.DataContext in favor of code-behind DataContext assignment to support constructor injection"
  - "TaskCompletionSource pattern for bridging overlay CaptureCompleted/Cancelled events to async capture methods"

patterns-established:
  - "Post-capture pipeline: OutputService.ProcessCaptureAsync then CapturePreviewPopup.Show"
  - "Overlay lifecycle: create VM with ICaptureService, create window, subscribe events via TCS, Show, await, Close"

requirements-completed: [CAPT-01, CAPT-02, CAPT-03, CAPT-04, CAPT-05, CAPT-06, OUT-01, OUT-02, OUT-03, OUT-04, OUT-05]

# Metrics
duration: 4min
completed: 2026-03-25
---

# Phase 2 Plan 5: End-to-End Integration Summary

**Complete capture-to-output pipeline: 4 capture modes wired through AppViewModel, TrayIcon, HotkeyService, OutputService, and preview popup with HistoryView in MainWindow**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-25T20:49:22Z
- **Completed:** 2026-03-25T20:52:57Z
- **Tasks:** 2 of 3 (Task 3 is human-verify checkpoint)
- **Files modified:** 7

## Accomplishments
- AppViewModel routes 4 capture mode commands (Region, Window, FullScreen, Freeze) through overlays and OutputService with CapturePreviewPopup
- TrayIcon NativeMenu expanded from single "Capture Screen" to 4 specific capture mode items
- HotkeyService wired to actual capture commands: Cmd+Shift+5 (Region), Cmd+Shift+4 (Window), Cmd+Shift+3 (Full Screen)
- Service initialization in App.axaml.cs: ScreenCaptureKitBridge, OutputService, SqliteCaptureHistory, AppServices container
- MainWindow embeds HistoryView with sidebar navigation and auto-load on window open

## Task Commits

Each task was committed atomically:

1. **Task 1: Wire AppViewModel with capture mode commands, TrayIcon menu, OutputService, and service initialization** - `5b7047c18` (feat)
2. **Task 2: Wire HotkeyService to trigger CaptureRegion and add secondary hotkeys** - `f2eef23c2` (feat)
3. **Task 3: Verify complete Phase 2 capture pipeline end-to-end** - PENDING (checkpoint:human-verify)

## Files Created/Modified
- `ShareXMac.App/ViewModels/AppViewModel.cs` - 4 capture ICommand properties, ProcessCaptureResult pipeline, AppServices constructor
- `ShareXMac.App/App.axaml` - NativeMenu with Capture Region/Window/Full Screen/Freeze & Capture items
- `ShareXMac.App/App.axaml.cs` - Service construction, AppServices record, HotkeyService wiring, updated GetOrCreateMainWindow
- `ShareXMac.App/ViewModels/MainWindowViewModel.cs` - HistoryViewModel property, ICaptureHistory constructor
- `ShareXMac.App/Views/MainWindow.axaml` - DockPanel with sidebar and views:HistoryView DataContext binding
- `ShareXMac.App/Views/MainWindow.axaml.cs` - OnOpened loads history via vm.History.LoadHistoryAsync()
- `ShareXMac.App/Services/HotkeyService.cs` - Start accepts Action callbacks, Cmd+Shift+3/4/5 dispatch on UI thread

## Decisions Made
- AppServices sealed record as poor-man's DI container (no framework needed for 4 services)
- Removed XAML Application.DataContext -- must use code-behind to pass services to AppViewModel constructor
- TaskCompletionSource bridges overlay event callbacks (CaptureCompleted/Cancelled) to async/await pattern

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Removed XAML-based DataContext that would fail at runtime**
- **Found during:** Task 1
- **Issue:** App.axaml had `<Application.DataContext><vm:AppViewModel /></Application.DataContext>` which creates AppViewModel via parameterless constructor. Since AppViewModel now requires AppServices parameter, this would throw at runtime.
- **Fix:** Removed XAML DataContext block; DataContext is now set in App.axaml.cs code-behind after service construction
- **Files modified:** ShareXMac.App/App.axaml
- **Verification:** App.axaml no longer references vm:AppViewModel in DataContext section
- **Committed in:** 5b7047c18

---

**Total deviations:** 1 auto-fixed (1 missing critical)
**Impact on plan:** Essential fix for runtime correctness. No scope creep.

## Issues Encountered
None - plan executed smoothly.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Complete Phase 2 capture pipeline ready for end-to-end testing (Task 3 checkpoint)
- All capture modes accessible from TrayIcon and keyboard shortcuts
- OutputService processes every capture (clipboard + file + history)
- HistoryView loads and displays captures in MainWindow
- Ready for Phase 3 (Annotation) once verification passes

## Self-Check: PASSED

All 7 modified files verified present. Both task commits (5b7047c18, f2eef23c2) verified in git log.

---
*Phase: 02-core-capture*
*Completed: 2026-03-25*
