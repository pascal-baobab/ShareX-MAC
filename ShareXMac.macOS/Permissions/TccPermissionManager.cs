using System.Runtime.InteropServices;
using ShareXMac.Core.Permissions;

namespace ShareXMac.macOS.Permissions;

/// <summary>
/// macOS implementation of IPermissionManager using CoreGraphics TCC APIs.
/// CGPreflightScreenCaptureAccess / CGRequestScreenCaptureAccess are the official
/// APIs for Screen Recording permission (not deprecated, not App Sandbox-blocked).
/// AXIsProcessTrusted() is used for Accessibility / Input Monitoring.
/// </summary>
public sealed class TccPermissionManager : IPermissionManager
{
    // CoreGraphics framework — used for screen recording TCC checks.
    // These symbols exist in macOS 10.15+ and are not deprecated.
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern bool CGPreflightScreenCaptureAccess();

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern bool CGRequestScreenCaptureAccess();

    // ApplicationServices framework — used for Accessibility / Input Monitoring check.
    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrusted();

    public PermissionStatus CheckScreenRecordingPermission()
    {
        // CGPreflightScreenCaptureAccess returns true if granted, false if denied OR not determined.
        // There is no separate "not determined" state exposed via this API — we treat false as Denied
        // for display purposes; the user can still trigger the request flow.
        return CGPreflightScreenCaptureAccess() ? PermissionStatus.Granted : PermissionStatus.Denied;
    }

    public async Task<PermissionStatus> RequestScreenRecordingPermissionAsync()
    {
        // CGRequestScreenCaptureAccess shows the system permission dialog.
        // It returns the result synchronously, but we wrap in Task.Run to avoid blocking
        // the UI thread while the dialog is open.
        // IMPORTANT: This must be invoked after NSApplication.Init() — the Avalonia
        // AppBuilder.Configure().UseMacOS() call handles this for the main path.
        bool granted = await Task.Run(() => CGRequestScreenCaptureAccess());
        return granted ? PermissionStatus.Granted : PermissionStatus.Denied;
    }

    public PermissionStatus CheckAccessibilityPermission()
    {
        return AXIsProcessTrusted() ? PermissionStatus.Granted : PermissionStatus.Denied;
    }

    public void OpenAccessibilitySettings()
    {
        // Deep-link to the Accessibility pane in System Settings.
        // x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility
        // is the confirmed deep-link URL that works on macOS 13+ (Ventura and later).
        var url = "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility";
        System.Diagnostics.Process.Start("open", url);
    }
}
