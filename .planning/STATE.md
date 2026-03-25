---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: Ready to execute
stopped_at: Completed 02-01-PLAN.md
last_updated: "2026-03-25T20:29:35.563Z"
progress:
  total_phases: 6
  completed_phases: 1
  total_plans: 8
  completed_plans: 3
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-25)

**Core value:** Reliable screen capture and instant sharing on macOS — capture anything, share it anywhere, in one keystroke.
**Current focus:** Phase 02 — core-capture

## Current Position

Phase: 02 (core-capture) — EXECUTING
Plan: 2 of 5

## Performance Metrics

**Velocity:**

- Total plans completed: 0
- Average duration: —
- Total execution time: —

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**

- Last 5 plans: —
- Trend: —

*Updated after each plan completion*
| Phase 01 P01 | 3min | 2 tasks | 16 files |
| Phase 01 P02 | 2min | 2 tasks | 5 files |
| Phase 01 P03 | 3min | 3 tasks | 11 files |
| Phase 02 P01 | 10min | 2 tasks | 12 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Pre-Phase 1]: Skip App Sandbox for v1 — distribute via Developer ID notarization; sandbox crashes Avalonia on Apple Silicon
- [Pre-Phase 1]: ScreenCaptureKit over CGWindowList — CGWindowListCreateImage deprecated in macOS 15
- [Pre-Phase 1]: SixLabors.ImageSharp replaces System.Drawing/GDI+ everywhere — GDI+ throws PlatformNotSupportedException on macOS at runtime; this is not a compile-time error
- [Phase 01]: Per-project Directory.Build.props to isolate ShareXMac from root Windows-specific build configuration
- [Phase 01]: CFBundleIdentifier=com.sharexmac.app as stable TCC identity for macOS permissions
- [Phase 01]: CGPreflightScreenCaptureAccess false maps to Denied (API does not distinguish NotDetermined)
- [Phase 01]: ImageSharp Bgra32 pixel format for CGImage-to-PNG conversion avoiding System.Drawing
- [Phase 01]: AppViewModel owns TrayIcon commands; uses IPermissionManager to gate capture behind permission check
- [Phase 01]: HotkeyService uses SharpHook TaskPoolGlobalHook with ModifierMask for Cmd+Shift+5 detection
- [Phase 02]: NSPasteboard clipboard via raw P/Invoke (objc_msgSend), not Process.Start pbcopy
- [Phase 02]: Inline JPEG thumbnails (100x100 max, 80% quality) in SQLite for capture history
- [Phase 02]: CaptureResult sealed record as universal output type for all capture modes

### Pending Todos

None yet.

### Blockers/Concerns

- [Phase 1 risk]: NSRunLoop hang — SCKit calls hang indefinitely without NSApplication.Init(); must initialize before any capture call; never call from Task.Run without run loop setup
- [Phase 1 risk]: GDI+ audit required before porting any ShareX library — runtime crash, not compile-time
- [Phase 1 risk]: JIT entitlement (com.apple.security.cs.allow-jit) must be in entitlements.plist from day one or notarized app crashes immediately
- [Phase 2 risk]: Transparent overlay click-through requires P/Invoke to NSWindow.ignoresMouseEvents — pure Avalonia cannot do this
- [Phase 2 risk]: SCContentSharingPicker C# invocation pattern is undocumented in Avalonia context — needs spike
- [Phase 3 concern]: macOS 15.4 global hotkey regression — hotkeys stop firing globally for some apps; build in-app menu fallbacks for every hotkey action
- [Phase 6 risk]: SCStream audio on macOS 15 has confirmed bug (SCStreamErrorDomain Code=-3805); treat audio as best-effort

## Session Continuity

Last session: 2026-03-25T20:29:35.560Z
Stopped at: Completed 02-01-PLAN.md
Resume file: None
