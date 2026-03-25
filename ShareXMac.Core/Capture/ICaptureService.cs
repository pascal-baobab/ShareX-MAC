namespace ShareXMac.Core.Capture;

/// <summary>
/// Abstraction over the native screen capture API.
/// Implemented by ScreenCaptureKitBridge in ShareXMac.macOS.
/// </summary>
public interface ICaptureService
{
    /// <summary>
    /// Takes a single full-screen screenshot of the primary display.
    /// Returns PNG-encoded bytes.
    /// Throws <see cref="CaptureException"/> if Screen Recording permission is not granted
    /// or if the ScreenCaptureKit call fails.
    ///
    /// IMPORTANT: Must be called from a context where NSApplication is running
    /// (i.e., after AppBuilder.Configure().UseMacOS() has executed) to avoid
    /// the NSRunLoop hang described in dotnet/macios#17350.
    /// </summary>
    Task<byte[]> TakeSingleScreenshotAsync(CancellationToken ct = default);
}
