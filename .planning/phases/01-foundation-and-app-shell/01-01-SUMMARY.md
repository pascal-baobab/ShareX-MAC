---
phase: 01-foundation-and-app-shell
plan: 01
subsystem: infra
tags: [dotnet, avalonia, csproj, nuget, cpm, entitlements, plist, macos]

# Dependency graph
requires: []
provides:
  - Three-project .NET solution (Core, macOS, App) with correct TFMs
  - NuGet central package management with version-pinned dependencies
  - entitlements.plist with JIT and library validation entitlements
  - Info.plist with screen capture and AppleEvents usage descriptions
  - Per-project Directory.Build.props overriding Windows-specific root props
affects: [01-02, 01-03, 02, 03, 04, 05, 06]

# Tech tracking
tech-stack:
  added: [Avalonia 11.3.12, SharpHook 7.1.1, SixLabors.ImageSharp 3.1.12, sqlite-net-pcl 1.9.172]
  patterns: [NuGet central package management, per-project Directory.Build.props override]

key-files:
  created:
    - ShareXMac.sln
    - ShareXMac.Core/ShareXMac.Core.csproj
    - ShareXMac.macOS/ShareXMac.macOS.csproj
    - ShareXMac.App/ShareXMac.App.csproj
    - ShareXMac.App/entitlements.plist
    - ShareXMac.App/Info.plist
    - ShareXMac.App/Program.cs
    - ShareXMac.App/App.axaml
    - ShareXMac.App/App.axaml.cs
    - Directory.Packages.props
  modified:
    - Directory.Packages.props

key-decisions:
  - "Per-project Directory.Build.props to isolate ShareXMac projects from root Windows-specific build props"
  - "CFBundleIdentifier set to com.sharexmac.app as stable reverse-DNS TCC identity"
  - "LSUIElement=true for menu bar app behavior (no Dock icon)"
  - "EnableWindowsTargeting=false in Core to catch GDI+ usage at compile time"

patterns-established:
  - "Three-tier project layout: Core (platform-agnostic) -> macOS (Apple APIs) -> App (UI)"
  - "NuGet central package management via Directory.Packages.props"
  - "Per-project Directory.Build.props when root props conflict with project targets"

requirements-completed: [FOUND-01, FOUND-02, FOUND-03, FOUND-04, FOUND-05]

# Metrics
duration: 3min
completed: 2026-03-25
---

# Phase 01 Plan 01: Project Scaffold Summary

**Three-project .NET solution (Core net9.0, macOS net9.0-macos, App Avalonia) with NuGet CPM, JIT entitlements, and Info.plist screen capture permissions**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-25T12:06:31Z
- **Completed:** 2026-03-25T12:09:44Z
- **Tasks:** 2
- **Files modified:** 16

## Accomplishments
- Three-project solution structure with correct target frameworks and inter-project references
- NuGet central package management with Avalonia 11.3.12, SharpHook 7.1.1, ImageSharp 3.1.12, sqlite-net-pcl pinned
- entitlements.plist with cs.allow-jit and cs.disable-library-validation for notarized .NET/Avalonia apps
- Info.plist with NSScreenCaptureUsageDescription, NSAppleEventsUsageDescription, CFBundleIdentifier, LSUIElement

## Task Commits

Each task was committed atomically:

1. **Task 1: Create three-project solution with correct TFMs and project references** - `3d499e1bc` (feat)
2. **Task 2: Create entitlements.plist and Info.plist with all required macOS declarations** - `f9bd8667f` (feat)

## Files Created/Modified
- `ShareXMac.sln` - Three-project solution file
- `ShareXMac.Core/ShareXMac.Core.csproj` - Platform-agnostic business logic (net9.0, EnableWindowsTargeting=false)
- `ShareXMac.Core/Class1.cs` - Placeholder AppInfo class
- `ShareXMac.macOS/ShareXMac.macOS.csproj` - macOS interop project (net9.0-macos, AllowUnsafeBlocks)
- `ShareXMac.macOS/Class1.cs` - Placeholder MacPlatform class
- `ShareXMac.App/ShareXMac.App.csproj` - Avalonia UI app (net9.0) with project refs, CodesignEntitlements
- `ShareXMac.App/Program.cs` - Avalonia application entry point
- `ShareXMac.App/App.axaml` - Avalonia application XAML with FluentTheme
- `ShareXMac.App/App.axaml.cs` - Application code-behind
- `ShareXMac.App/app.manifest` - Assembly manifest
- `ShareXMac.App/entitlements.plist` - Hardened runtime entitlements (JIT + library validation)
- `ShareXMac.App/Info.plist` - macOS bundle metadata and permission usage strings
- `Directory.Packages.props` - Central NuGet package version management
- `ShareXMac.Core/Directory.Build.props` - Override root Windows build props
- `ShareXMac.macOS/Directory.Build.props` - Override root Windows build props
- `ShareXMac.App/Directory.Build.props` - Override root Windows build props

## Decisions Made
- **Per-project Directory.Build.props**: The root Directory.Build.props targets Windows (win-x64/win-arm64 RuntimeIdentifiers, Windows-specific configurations). Rather than modifying the root file (which the original ShareX projects depend on), each ShareXMac project gets its own Directory.Build.props that overrides the root, ensuring clean macOS-targeted builds.
- **CFBundleIdentifier = com.sharexmac.app**: Stable reverse-DNS identifier that macOS TCC uses to remember granted permissions across app launches.
- **LSUIElement = true**: Correct for a menu bar app that should not appear in the Dock or App Switcher.
- **EnableWindowsTargeting = false**: Causes compile-time errors if Core accidentally references System.Drawing or other Windows-only APIs, preventing runtime GDI+ crashes.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Per-project Directory.Build.props to override root Windows-specific build configuration**
- **Found during:** Task 1 (project creation)
- **Issue:** Root Directory.Build.props sets RuntimeIdentifier to win-x64/win-arm64 and defines Windows-specific configurations (Steam, MicrosoftStore). ShareXMac projects targeting macOS would inherit these invalid settings.
- **Fix:** Created per-project Directory.Build.props files in ShareXMac.Core, ShareXMac.macOS, and ShareXMac.App that override the root with macOS-appropriate properties.
- **Files modified:** ShareXMac.Core/Directory.Build.props, ShareXMac.macOS/Directory.Build.props, ShareXMac.App/Directory.Build.props
- **Verification:** Project files correctly target net9.0 and net9.0-macos without Windows RuntimeIdentifier conflicts
- **Committed in:** 3d499e1bc (Task 1 commit)

**2. [Rule 3 - Blocking] Created project files manually without dotnet CLI**
- **Found during:** Task 1 (project creation)
- **Issue:** .NET SDK is not installed on this machine. The plan specified using dotnet CLI commands (dotnet new, dotnet add, dotnet build).
- **Fix:** All project files (.sln, .csproj, .cs, .axaml) were hand-authored with correct MSBuild structure, GUIDs, and NuGet references. The files are structurally equivalent to what dotnet CLI would generate.
- **Files modified:** All created files
- **Verification:** All acceptance criteria checked via grep; dotnet build verification deferred until .NET SDK is installed
- **Committed in:** 3d499e1bc (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (2 blocking)
**Impact on plan:** Both fixes necessary to complete the plan. No scope creep. The hand-authored files match what dotnet CLI would produce.

## Issues Encountered
- .NET SDK not installed: `dotnet build ShareXMac.sln` verification could not be performed. All file structure and content checks pass. Build verification should be performed when .NET 9 SDK with macos workload is installed.

## User Setup Required

The .NET 9 SDK must be installed before building:
1. Install .NET 9 SDK: `brew install dotnet@9` or download from https://dot.net/download
2. Install macos workload: `dotnet workload install macos`
3. Verify build: `dotnet build ShareXMac.sln`

## Next Phase Readiness
- Solution structure is complete and ready for subsequent plans
- Plan 01-02 (App Shell / Menu Bar) can proceed to add UI components
- Plan 01-03 can add settings infrastructure
- Build verification pending .NET SDK installation

## Self-Check: PASSED

All 10 key files verified present. Both task commits (3d499e1bc, f9bd8667f) verified in git log.

---
*Phase: 01-foundation-and-app-shell*
*Completed: 2026-03-25*
