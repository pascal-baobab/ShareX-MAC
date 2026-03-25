---
created: "2026-03-25T21:11:31.685Z"
title: Check all phases - full build and integration check
area: general
files:
  - ShareXMac.App/ShareXMac.App.csproj
  - ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs
  - ShareXMac.App/Views/RegionSelectorWindow.axaml.cs
  - ShareXMac.App/Views/WindowPickerOverlay.axaml.cs
  - ShareXMac.App/ViewModels/HistoryViewModel.cs
  - ShareXMac.App/ViewModels/AppViewModel.cs
  - ShareXMac.App/App.axaml.cs
---

## Problem

After Phase 2 autonomous execution, the app launches but gets stuck in the permission setup flow. Multiple build errors were found post-merge (Avalonia API incompatibilities: TryGetFeature generic vs non-generic, duplicate Key.Enter/Key.Return case, lambda parameter shadowing discard). These were fixed but the full integration has not been validated end-to-end. The user reports the system stays blocked in setup — likely the PermissionWizard flow is not properly transitioning to the main capture pipeline.

Need a comprehensive check:
1. Build verification across all 3 projects (Core, macOS, App)
2. Permission flow: does it complete and transition to ready state?
3. TrayIcon menu: are all 4 capture modes visible and clickable?
4. Capture pipeline: does at least one mode produce output?
5. History/Preview: do the new UI components render?

## Solution

1. Run `dotnet build` on all projects — verify 0 errors
2. Audit the permission wizard flow in App.axaml.cs — check if it blocks indefinitely
3. Check AppViewModel initialization — ensure capture services are registered and commands are wired
4. Test each capture mode individually
5. Verify HistoryView loads and displays records
