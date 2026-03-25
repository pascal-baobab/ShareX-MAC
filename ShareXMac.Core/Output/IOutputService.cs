namespace ShareXMac.Core.Output;

using ShareXMac.Core.Capture;

/// <summary>
/// Handles clipboard copy, file save, and history insertion after a successful capture.
/// Implemented by OutputService in ShareXMac.macOS.
/// </summary>
public interface IOutputService
{
    /// <summary>
    /// Executes the configured output actions (clipboard, file save) for the given result.
    /// Returns the full path of the saved file, or null if SaveToFile=false.
    /// Always inserts into capture history regardless of other options.
    /// </summary>
    Task<string?> ProcessCaptureAsync(CaptureResult result, OutputOptions? options = null, CancellationToken ct = default);
}
