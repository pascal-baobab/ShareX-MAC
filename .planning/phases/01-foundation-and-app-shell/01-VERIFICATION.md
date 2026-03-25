---
phase: 01-foundation-and-app-shell
verified: 2026-03-25T13:00:00Z
status: gaps_found
score: 3/5 must-haves verified
gaps:
  - truth: "dotnet build ShareXMac.sln exits 0 with no errors on macOS"
    status: failed
    reason: ".NET SDK is not installed on the machine. All files were hand-authored and verified only by grep, never by dotnet build. Build verification is a hard requirement for this phase because hand-authored csproj, XAML, and net9.0-macos interop code can contain subtle errors (wrong namespace for ScreenCaptureKit bindings, invalid XAML, missing NuGet restore) that only surface at compile time."
    artifacts:
      - path: "ShareXMac.sln"
        issue: "Never compiled -- dotnet build not run"
    missing:
      - "Install .NET 9 SDK (brew install dotnet@9 or download from dot.net)"
      - "Install macos workload (dotnet workload install macos)"
      - "Run dotnet build ShareXMac.sln and fix any compilation errors"
  - truth: "App launches and runs as menu bar icon with no Dock icon, NativeMenu works, hotkey fires in background"
    status: failed
    reason: "No runtime verification has been performed. The app has never been launched. Plan 03 included a human verification checkpoint (9 manual checks) that was auto-approved without actually running the app. Without dotnet build succeeding first, runtime behavior cannot be tested."
    artifacts:
      - path: "ShareXMac.App/Program.cs"
        issue: "Never executed"
    missing:
      - "Build the solution successfully"
      - "Run the app and perform the 9-point manual verification from Plan 03"
human_verification:
  - test: "Launch app and verify no Dock icon appears (only menu bar icon)"
    expected: "TrayIcon visible in menu bar, no Dock icon"
    why_human: "LSUIElement behavior requires visual confirmation at runtime"
  - test: "Click menu bar icon and verify dropdown shows Capture Screen, Settings..., Quit"
    expected: "NativeMenu renders with all three items and separators"
    why_human: "NativeMenu rendering requires live macOS runtime"
  - test: "Click Settings... and verify MainWindow opens"
    expected: "Window opens with sidebar and placeholder content"
    why_human: "Window creation and show logic requires runtime"
  - test: "Verify PermissionWizard opens automatically when Screen Recording is not granted"
    expected: "Wizard window appears with Grant Screen Recording and Open Accessibility Settings buttons"
    why_human: "TCC permission check and wizard auto-launch require runtime"
  - test: "Click Grant Screen Recording button in wizard"
    expected: "macOS system permission dialog appears"
    why_human: "CGRequestScreenCaptureAccess P/Invoke requires runtime"
  - test: "Click Open Accessibility Settings button in wizard"
    expected: "System Settings opens to Accessibility pane"
    why_human: "Deep-link URL handling requires runtime"
  - test: "Press Cmd+Shift+5 with app in background"
    expected: "Console shows [HotkeyService] Hotkey fired: CaptureScreen"
    why_human: "Global hotkey via CGEventTap requires runtime with Accessibility permission"
  - test: "Click Quit from menu"
    expected: "App exits cleanly"
    why_human: "Application lifecycle requires runtime"
---

# Phase 1: Foundation and App Shell Verification Report

**Phase Goal:** A notarized Avalonia macOS app runs from the menu bar, requests permissions correctly, registers a global hotkey, and can take one confirmed screenshot via ScreenCaptureKit
**Verified:** 2026-03-25T13:00:00Z
**Status:** gaps_found
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | App launches as TrayIcon in menu bar with NativeMenu dropdown | ? UNCERTAIN | App.axaml contains TrayIcon + NativeMenu with Capture Screen, Settings..., Quit items. Info.plist has LSUIElement=true. But app has never been built or launched. |
| 2 | Settings/history window opens from menu bar | ? UNCERTAIN | MainWindow.axaml exists with sidebar skeleton. AppViewModel.OpenSettingsCommand calls GetOrCreateMainWindow(). Never runtime tested. |
| 3 | Screen Recording permission wizard appears on first launch with recoverable denial | ? UNCERTAIN | PermissionWizardWindow.axaml has Grant Screen Recording button bound to GrantScreenRecordingCommand. PermissionWizardViewModel calls RequestScreenRecordingPermissionAsync(). App.axaml.cs checks permission on startup and shows wizard if not granted. Code is structurally correct but never executed. |
| 4 | Accessibility permission step has working deep-link to System Settings | ? UNCERTAIN | PermissionWizardWindow.axaml has Open Accessibility Settings button. PermissionWizardViewModel calls OpenAccessibilitySettings() which calls Process.Start("open", url). Never runtime tested. |
| 5 | Global hotkey fires callback when app is in background | ? UNCERTAIN | HotkeyService.cs uses SharpHook TaskPoolGlobalHook, checks for Cmd+Shift+5 via ModifierMask.LeftMeta + ModifierMask.Shift + KeyCode.Vc5. Logs to console on match. Never runtime tested. |

**Score:** 0/5 truths fully verified (all UNCERTAIN due to no build/runtime verification)

### Required Artifacts

**Plan 01 Artifacts (Solution Scaffold):**

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `ShareXMac.sln` | Three-project solution | VERIFIED (exists, substantive) | Contains Core, macOS, App project references with correct GUIDs |
| `ShareXMac.Core/ShareXMac.Core.csproj` | net9.0 classlib with EnableWindowsTargeting=false | VERIFIED (exists, substantive) | net9.0 TFM, EnableWindowsTargeting=false, ImageSharp + sqlite-net-pcl packages |
| `ShareXMac.macOS/ShareXMac.macOS.csproj` | net9.0-macos classlib | VERIFIED (exists, substantive) | net9.0-macos TFM, AllowUnsafeBlocks=true, Core project reference |
| `ShareXMac.App/ShareXMac.App.csproj` | Avalonia app with project refs | VERIFIED (exists, substantive) | References Core + macOS projects, Avalonia + SharpHook packages, CodesignEntitlements |
| `ShareXMac.App/entitlements.plist` | JIT + library validation entitlements | VERIFIED (exists, substantive) | com.apple.security.cs.allow-jit and cs.disable-library-validation both present |
| `ShareXMac.App/Info.plist` | Bundle metadata + permission strings | VERIFIED (exists, substantive) | NSScreenCaptureUsageDescription, NSAppleEventsUsageDescription, LSUIElement=true, CFBundleIdentifier=com.sharexmac.app, LSMinimumSystemVersion=12.3 |
| `Directory.Packages.props` | Central package management | VERIFIED (exists, substantive) | ManagePackageVersionsCentrally=true, Avalonia 11.3.12, SharpHook 7.1.1, ImageSharp 3.1.12 pinned |

**Plan 02 Artifacts (Native Interop):**

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `ShareXMac.Core/Permissions/IPermissionManager.cs` | Permission abstraction | VERIFIED (exists, substantive, wired) | PermissionStatus enum + IPermissionManager interface with 4 methods |
| `ShareXMac.Core/Capture/ICaptureService.cs` | Capture abstraction | VERIFIED (exists, substantive, wired) | Task<byte[]> TakeSingleScreenshotAsync with CancellationToken |
| `ShareXMac.Core/Capture/CaptureException.cs` | Exception type | VERIFIED (exists, substantive) | Sealed exception with message and inner-exception constructors |
| `ShareXMac.macOS/Permissions/TccPermissionManager.cs` | TCC P/Invoke implementation | VERIFIED (exists, substantive, wired) | Implements IPermissionManager, DllImport for CGPreflightScreenCaptureAccess, CGRequestScreenCaptureAccess, AXIsProcessTrusted |
| `ShareXMac.macOS/Capture/ScreenCaptureKitBridge.cs` | ScreenCaptureKit capture | VERIFIED (exists, substantive, wired) | Implements ICaptureService, uses SCShareableContent.GetShareableContentAsync + SCScreenshotManager.CaptureImageAsync + ImageSharp PNG encoding |

**Plan 03 Artifacts (App Shell):**

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `ShareXMac.App/App.axaml` | TrayIcon with NativeMenu | VERIFIED (exists, substantive) | TrayIcon with 3 NativeMenuItems bound to AppViewModel commands |
| `ShareXMac.App/App.axaml.cs` | Startup with permission check + hotkey init | VERIFIED (exists, substantive, wired) | Creates HotkeyService, calls CheckPermissionsOnStartup, shows PermissionWizardWindow if needed |
| `ShareXMac.App/Program.cs` | Entry point with AppBuilder | VERIFIED (exists, substantive) | AppBuilder.Configure<App>().UsePlatformDetect().UseReactiveUI() |
| `ShareXMac.App/ViewModels/AppViewModel.cs` | TrayIcon ViewModel | VERIFIED (exists, substantive, wired) | ReactiveCommands for capture, settings, quit. Uses TccPermissionManager. |
| `ShareXMac.App/Services/HotkeyService.cs` | SharpHook global hotkey | VERIFIED (exists, substantive, wired) | TaskPoolGlobalHook, Cmd+Shift+5 detection, console log on fire |
| `ShareXMac.App/Views/PermissionWizardWindow.axaml` | Permission onboarding UI | VERIFIED (exists, substantive) | Screen Recording + Accessibility steps with buttons and status labels |
| `ShareXMac.App/Views/PermissionWizardWindow.axaml.cs` | Wizard code-behind | VERIFIED (exists, substantive, wired) | Accepts IPermissionManager, creates PermissionWizardViewModel |
| `ShareXMac.App/ViewModels/PermissionWizardViewModel.cs` | Wizard ViewModel | VERIFIED (exists, substantive, wired) | GrantScreenRecordingAsync calls RequestScreenRecordingPermissionAsync, OpenAccessibilitySettings calls OpenAccessibilitySettings |
| `ShareXMac.App/Views/MainWindow.axaml` | Settings window skeleton | VERIFIED (exists, substantive) | Window with sidebar (History, Settings labels) and placeholder content area |

### Key Link Verification

**Plan 01 Key Links:**

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| ShareXMac.App.csproj | ShareXMac.macOS.csproj | ProjectReference | WIRED | `<ProjectReference Include="..\ShareXMac.macOS\ShareXMac.macOS.csproj" />` present |
| ShareXMac.App.csproj | ShareXMac.Core.csproj | ProjectReference | WIRED | `<ProjectReference Include="..\ShareXMac.Core\ShareXMac.Core.csproj" />` present |
| ShareXMac.macOS.csproj | ShareXMac.Core.csproj | ProjectReference | WIRED | `<ProjectReference Include="..\ShareXMac.Core\ShareXMac.Core.csproj" />` present |

**Plan 02 Key Links:**

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| TccPermissionManager.cs | IPermissionManager.cs | implements IPermissionManager | WIRED | `class TccPermissionManager : IPermissionManager` confirmed |
| ScreenCaptureKitBridge.cs | ICaptureService.cs | implements ICaptureService | WIRED | `class ScreenCaptureKitBridge : ICaptureService` confirmed |

**Plan 03 Key Links:**

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| App.axaml.cs | HotkeyService.cs | instantiation in OnFrameworkInitializationCompleted | WIRED | `new Services.HotkeyService()` + `.Start()` on line 32-33 |
| App.axaml.cs | TccPermissionManager.cs | instantiation and permission check | WIRED | `new TccPermissionManager()` on line 15, `CheckScreenRecordingPermission()` on line 44 |
| App.axaml | AppViewModel.cs | DataContext binding | WIRED | `<Application.DataContext><vm:AppViewModel /></Application.DataContext>` present |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| PermissionWizardViewModel.cs | ScreenRecordingStatus | IPermissionManager.CheckScreenRecordingPermission() | P/Invoke to CGPreflightScreenCaptureAccess (real API) | FLOWING (at runtime) |
| PermissionWizardViewModel.cs | AccessibilityStatus | IPermissionManager.CheckAccessibilityPermission() | P/Invoke to AXIsProcessTrusted (real API) | FLOWING (at runtime) |
| ScreenCaptureKitBridge.cs | screenshot bytes | SCScreenshotManager.CaptureImageAsync | ScreenCaptureKit native API | FLOWING (at runtime) |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Solution builds | `dotnet build ShareXMac.sln` | .NET SDK not installed | ? SKIP |
| App launches | `dotnet run --project ShareXMac.App` | .NET SDK not installed | ? SKIP |

Step 7b: SKIPPED -- .NET SDK is not installed on this machine. No runnable entry points available.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| FOUND-01 | 01-01, 01-03 | App launches as menu bar icon with dropdown menu | ? NEEDS HUMAN | App.axaml has TrayIcon + NativeMenu. Info.plist has LSUIElement=true. Code is structurally complete but never built or run. |
| FOUND-02 | 01-01, 01-03 | App has full settings/history window from menu bar | ? NEEDS HUMAN | MainWindow.axaml exists with skeleton layout. OpenSettingsCommand wired in AppViewModel. Never runtime tested. |
| FOUND-03 | 01-02, 01-03 | Screen Recording permission requested on first launch | ? NEEDS HUMAN | TccPermissionManager uses CGRequestScreenCaptureAccess. PermissionWizard auto-shows on startup when not granted. Never runtime tested. |
| FOUND-04 | 01-02, 01-03 | Accessibility permission requested with guidance | ? NEEDS HUMAN | TccPermissionManager uses AXIsProcessTrusted. Wizard has Open Accessibility Settings button with deep-link. Never runtime tested. |
| FOUND-05 | 01-03 | Global hotkeys trigger capture in background | ? NEEDS HUMAN | HotkeyService uses SharpHook TaskPoolGlobalHook with Cmd+Shift+5 detection. Never runtime tested. |

All 5 requirements are structurally addressed in code. None can be confirmed without build + runtime verification.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| ShareXMac.App/ViewModels/AppViewModel.cs | 38-39 | "Phase 1 placeholder" -- capture logs instead of capturing | Info | Intentional -- Phase 2 wires actual capture. Menu item still gates on permission check. |
| ShareXMac.App/Views/MainWindow.axaml | 21 | "coming in Phase 3" placeholder text | Info | Intentional -- MainWindow is a skeleton for Phase 1. Window opens and renders. |
| ShareXMac.App/ViewModels/MainWindowViewModel.cs | 7-8 | Empty ViewModel with placeholder comment | Info | Intentional -- populated in Phase 3. |
| ShareXMac.Core/Class1.cs | 4 | Placeholder class | Warning | Leftover from project scaffold. Should be removed or replaced. |
| ShareXMac.macOS/Class1.cs | 4 | Placeholder class | Warning | Leftover from project scaffold. Should be removed or replaced. |

No blocker anti-patterns found. The Phase 1 placeholders (capture action logs, MainWindow content) are intentional and documented. The Class1.cs files in Core and macOS are scaffold leftovers that should be cleaned up but do not block goal achievement.

### Human Verification Required

### 1. Build Verification

**Test:** Install .NET 9 SDK, run `dotnet workload install macos`, then `dotnet build ShareXMac.sln`
**Expected:** Build succeeds with 0 errors
**Why human:** .NET SDK not installed on this machine

### 2. App Launch and Menu Bar

**Test:** Run `dotnet run --project ShareXMac.App` and observe macOS menu bar and Dock
**Expected:** TrayIcon appears in menu bar, no Dock icon visible, dropdown shows Capture Screen / Settings... / Quit
**Why human:** LSUIElement, TrayIcon, and NativeMenu behavior require live macOS runtime

### 3. Permission Wizard Auto-Launch

**Test:** Run app with Screen Recording not granted (revoke in System Settings > Privacy > Screen Recording if needed)
**Expected:** PermissionWizardWindow opens automatically showing Screen Recording and Accessibility steps
**Why human:** TCC permission state check and window auto-show require runtime

### 4. Grant Screen Recording Button

**Test:** Click "Grant Screen Recording" in the wizard
**Expected:** macOS system permission dialog appears
**Why human:** CGRequestScreenCaptureAccess P/Invoke triggers OS dialog

### 5. Open Accessibility Settings Button

**Test:** Click "Open Accessibility Settings" in the wizard
**Expected:** System Settings opens to Accessibility privacy pane
**Why human:** Deep-link URL handling is OS-level behavior

### 6. Global Hotkey (Cmd+Shift+5)

**Test:** With app running in background and Accessibility permission granted, press Cmd+Shift+5
**Expected:** Terminal shows `[HotkeyService] Hotkey fired: CaptureScreen (Cmd+Shift+5)`
**Why human:** SharpHook CGEventTap requires Accessibility permission and runtime verification

### 7. Settings Window

**Test:** Click "Settings..." from TrayIcon dropdown menu
**Expected:** MainWindow opens with sidebar showing ShareXMac, History, Settings labels
**Why human:** Window creation from menu command requires runtime

### 8. Quit

**Test:** Click "Quit ShareXMac" from TrayIcon dropdown menu
**Expected:** App exits cleanly with no crash
**Why human:** Application lifecycle requires runtime

## Gaps Summary

**The single root cause blocking all 5 truths: .NET SDK is not installed.**

All artifacts exist, are substantive, and are correctly wired at the code level. The solution structure, project references, interface implementations, XAML bindings, entitlements, Info.plist, and NuGet configuration all match the plan specifications.

However, none of this code has ever been compiled or executed. The phase goal specifies "A notarized Avalonia macOS app **runs** from the menu bar" -- the word "runs" requires at minimum a successful build and launch. Without the .NET 9 SDK installed, verification cannot proceed past the artifact existence level.

**Specific risk areas that build verification would catch:**
1. ScreenCaptureKit namespace availability in net9.0-macos bindings (SCShareableContent, SCScreenshotManager, SCContentFilter, SCStreamConfiguration may have different API shapes in the actual dotnet/macios bindings)
2. SharpHook 7.1.1 API shape (KeyCode.Vc5, ModifierMask.LeftMeta, KeyboardHookEventArgs.RawEvent.Mask may differ)
3. Avalonia XAML parsing of TrayIcon + NativeMenu (namespace resolution, command binding)
4. NuGet restore with central package management (version conflicts between existing ShareX packages and new ShareXMac packages)

**Secondary issue:** The Class1.cs placeholder files in Core and macOS projects are harmless but should be cleaned up.

**To close these gaps:**
1. Install .NET 9 SDK and macos workload
2. Run `dotnet build ShareXMac.sln` and fix any compilation errors
3. Run the app and complete the 8-point human verification checklist above

---

_Verified: 2026-03-25T13:00:00Z_
_Verifier: Claude (gsd-verifier)_
