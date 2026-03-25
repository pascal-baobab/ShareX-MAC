namespace ShareXMac.Core.Capture;

/// <summary>Identifies what was captured and which capture path was used.</summary>
public enum CaptureMode
{
    FullScreen,   // Entire active monitor
    AllScreens,   // All connected monitors stitched
    Region,       // User-selected rectangular region
    Window,       // Specific application window
    Freeze        // Frozen screen region (for capturing transient UI)
}
