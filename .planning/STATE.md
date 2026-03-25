# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-25)

**Core value:** Reliable screen capture and instant sharing on macOS — capture anything, share it anywhere, in one keystroke.
**Current focus:** Phase 1 — Foundation and App Shell

## Current Position

Phase: 1 of 6 (Foundation and App Shell)
Plan: 0 of ? in current phase
Status: Ready to plan
Last activity: 2026-03-25 — Roadmap created; all 46 v1 requirements mapped to 6 phases

Progress: [░░░░░░░░░░] 0%

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

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Pre-Phase 1]: Skip App Sandbox for v1 — distribute via Developer ID notarization; sandbox crashes Avalonia on Apple Silicon
- [Pre-Phase 1]: ScreenCaptureKit over CGWindowList — CGWindowListCreateImage deprecated in macOS 15
- [Pre-Phase 1]: SixLabors.ImageSharp replaces System.Drawing/GDI+ everywhere — GDI+ throws PlatformNotSupportedException on macOS at runtime; this is not a compile-time error

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

Last session: 2026-03-25
Stopped at: Roadmap created — ready to run /gsd:plan-phase 1
Resume file: None
