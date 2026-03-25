namespace ShareXMac.Core.Permissions;

public enum PermissionStatus
{
    /// <summary>Permission has been granted by the user.</summary>
    Granted,
    /// <summary>Permission has been explicitly denied by the user.</summary>
    Denied,
    /// <summary>Permission has not yet been requested (first launch).</summary>
    NotDetermined
}

/// <summary>
/// Abstraction over macOS TCC permission checks and requests.
/// Implemented by TccPermissionManager in ShareXMac.macOS.
/// </summary>
public interface IPermissionManager
{
    /// <summary>
    /// Checks current Screen Recording (NSScreenCapture) permission state without
    /// triggering a system prompt. Safe to call at any time.
    /// Maps to CGPreflightScreenCaptureAccess() on macOS.
    /// </summary>
    PermissionStatus CheckScreenRecordingPermission();

    /// <summary>
    /// Requests Screen Recording permission, triggering a system dialog if not yet
    /// determined. Returns the final status after the user responds.
    /// Maps to CGRequestScreenCaptureAccess() on macOS.
    /// </summary>
    Task<PermissionStatus> RequestScreenRecordingPermissionAsync();

    /// <summary>
    /// Checks current Accessibility / Input Monitoring permission state.
    /// Maps to AXIsProcessTrusted() on macOS.
    /// </summary>
    PermissionStatus CheckAccessibilityPermission();

    /// <summary>
    /// Opens System Settings to the Accessibility privacy pane.
    /// Accessibility permission cannot be requested programmatically — the user
    /// must enable it manually in Settings. This method deep-links there.
    /// </summary>
    void OpenAccessibilitySettings();
}
