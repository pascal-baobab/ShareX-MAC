namespace ShareXMac.Core.Capture;

/// <summary>
/// Immutable result of a completed capture operation.
/// PngBytes is always PNG-encoded. Never null on success.
/// </summary>
public sealed record CaptureResult(
    byte[] PngBytes,
    CaptureMode Mode,
    DateTimeOffset CapturedAt,
    int Width,
    int Height,
    /// <summary>CoreGraphics display ID (0 = primary). Used for multi-monitor coordinate mapping.</summary>
    uint DisplayId = 0
);
