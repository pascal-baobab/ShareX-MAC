namespace ShareXMac.Core.Capture;

/// <summary>
/// Abstraction over the native screen capture API.
/// Implemented by ScreenCaptureKitBridge in ShareXMac.macOS.
/// All methods must be called after NSApplication.Init() (i.e., from Avalonia OnFrameworkInitializationCompleted
/// or later) — earlier calls hang on the NSRunLoop.
/// </summary>
public interface ICaptureService
{
    /// <summary>Captures the full active display. Returns CaptureResult with Mode=FullScreen.</summary>
    Task<CaptureResult> CaptureFullScreenAsync(uint displayId = 0, CancellationToken ct = default);

    /// <summary>Captures all connected displays stitched into a single image. Returns Mode=AllScreens.</summary>
    Task<CaptureResult> CaptureAllScreensAsync(CancellationToken ct = default);

    /// <summary>
    /// Captures a specific pixel rectangle on the given display.
    /// rect is in Cocoa screen coordinates (origin bottom-left, y increases upward).
    /// Returns Mode=Region.
    /// </summary>
    Task<CaptureResult> CaptureRegionAsync(CGRectCapture rect, uint displayId = 0, CancellationToken ct = default);

    /// <summary>
    /// Captures a specific window by its CoreGraphics window ID.
    /// includeShadow=false uses kCGWindowImageBoundsIgnoreFraming.
    /// Returns Mode=Window.
    /// </summary>
    Task<CaptureResult> CaptureWindowAsync(uint windowId, bool includeShadow = true, CancellationToken ct = default);

    /// <summary>
    /// Captures the entire screen as a frozen bitmap (for use as freeze-capture overlay).
    /// The caller displays this as a fullscreen overlay and then calls CaptureRegionAsync.
    /// Returns Mode=Freeze.
    /// </summary>
    Task<CaptureResult> CaptureFreezeAsync(uint displayId = 0, CancellationToken ct = default);

    /// <summary>
    /// Enumerates all on-screen windows suitable for window capture.
    /// Excludes desktop elements. Returns list ordered front-to-back.
    /// </summary>
    Task<IReadOnlyList<WindowInfo>> GetWindowListAsync(CancellationToken ct = default);

    /// <summary>Returns all active display IDs via CGGetActiveDisplayList.</summary>
    uint[] GetActiveDisplayIds();
}

/// <summary>Screen-coordinate rectangle for region capture. Origin is top-left (Avalonia convention).</summary>
public readonly record struct CGRectCapture(double X, double Y, double Width, double Height);

/// <summary>Metadata about an on-screen window for window-picker UI.</summary>
public sealed record WindowInfo(
    uint WindowId,
    string OwnerName,
    string? WindowName,
    double X, double Y, double Width, double Height,
    int Layer
);
