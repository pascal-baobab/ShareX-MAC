namespace ShareXMac.Core.Capture;

/// <summary>Thrown when a capture operation fails for any reason.</summary>
public sealed class CaptureException : Exception
{
    public CaptureException(string message) : base(message) { }
    public CaptureException(string message, Exception inner) : base(message, inner) { }
}
