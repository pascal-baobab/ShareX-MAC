using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ShareXMac.Core.Capture;

namespace ShareXMac.App.ViewModels;

/// <summary>
/// ViewModel for the window picker overlay. Manages the window list from
/// ICaptureService.GetWindowListAsync, cursor hit-testing against window bounds,
/// shadow toggle, and window capture via CaptureWindowAsync.
/// </summary>
public sealed class WindowPickerViewModel : ReactiveObject
{
    private readonly ICaptureService _captureService;

    private double _cursorX;
    private double _cursorY;
    private WindowInfo? _hoveredWindow;
    private bool _includeShadow = true;

    public WindowPickerViewModel(ICaptureService captureService)
    {
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
    }

    // --- Observable properties ---

    public double CursorX
    {
        get => _cursorX;
        set => this.RaiseAndSetIfChanged(ref _cursorX, value);
    }

    public double CursorY
    {
        get => _cursorY;
        set => this.RaiseAndSetIfChanged(ref _cursorY, value);
    }

    public WindowInfo? HoveredWindow
    {
        get => _hoveredWindow;
        set => this.RaiseAndSetIfChanged(ref _hoveredWindow, value);
    }

    public bool IncludeShadow
    {
        get => _includeShadow;
        set => this.RaiseAndSetIfChanged(ref _includeShadow, value);
    }

    /// <summary>The list of on-screen windows, fetched once when overlay opens.</summary>
    public IReadOnlyList<WindowInfo> Windows { get; private set; } = Array.Empty<WindowInfo>();

    /// <summary>
    /// Screen height used for Cocoa-to-Avalonia coordinate conversion.
    /// Set by the overlay window on open.
    /// </summary>
    public double ScreenHeight { get; set; }

    // --- Result and events ---

    /// <summary>The capture result. Null if cancelled.</summary>
    public CaptureResult? Result { get; private set; }

    /// <summary>Fired when a window is captured successfully.</summary>
    public event Action? CaptureCompleted;

    /// <summary>Fired when the user cancels the picker.</summary>
    public event Action? Cancelled;

    // --- Initialization ---

    /// <summary>
    /// Fetches the window list from ICaptureService. Call once when overlay opens.
    /// </summary>
    public async Task LoadWindowsAsync()
    {
        try
        {
            Windows = await _captureService.GetWindowListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowPicker] Failed to load windows: {ex.Message}");
            Windows = Array.Empty<WindowInfo>();
        }
    }

    // --- Mouse tracking with hit-testing ---

    /// <summary>
    /// Updates cursor position and determines which window (if any) is under the cursor.
    /// Converts Cocoa coordinates (bottom-left origin) to Avalonia coordinates (top-left origin)
    /// for hit-testing.
    /// </summary>
    public void UpdateCursorPosition(double x, double y)
    {
        CursorX = x;
        CursorY = y;

        // Hit-test: find the frontmost window whose Avalonia-converted bounds contain (x, y).
        // WindowInfo coordinates from CGWindowListCopyWindowInfo use Cocoa origin (top-left for CGWindowList).
        // CGWindowListCopyWindowInfo actually uses a top-left origin (unlike most Cocoa APIs),
        // so no coordinate conversion is needed for hit-testing against Avalonia coordinates.
        HoveredWindow = Windows.FirstOrDefault(w =>
            x >= w.X && x <= w.X + w.Width &&
            y >= w.Y && y <= w.Y + w.Height);
    }

    /// <summary>
    /// Gets the Avalonia-space bounds for a given WindowInfo.
    /// CGWindowListCopyWindowInfo returns window bounds with top-left origin (same as Avalonia),
    /// so coordinates are used directly.
    /// </summary>
    public (double X, double Y, double Width, double Height) GetWindowBounds(WindowInfo window)
    {
        return (window.X, window.Y, window.Width, window.Height);
    }

    // --- Actions ---

    /// <summary>
    /// Captures the currently hovered window. Calls CaptureWindowAsync with shadow toggle.
    /// </summary>
    public async Task SelectWindow()
    {
        if (HoveredWindow == null) return;

        try
        {
            Result = await _captureService.CaptureWindowAsync(HoveredWindow.WindowId, IncludeShadow);
            CaptureCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowPicker] Capture failed: {ex.Message}");
            Cancel();
        }
    }

    /// <summary>Toggles whether window capture includes the shadow.</summary>
    public void ToggleShadow()
    {
        IncludeShadow = !IncludeShadow;
    }

    /// <summary>Cancels the picker without capturing.</summary>
    public void Cancel()
    {
        Result = null;
        Cancelled?.Invoke();
    }
}
