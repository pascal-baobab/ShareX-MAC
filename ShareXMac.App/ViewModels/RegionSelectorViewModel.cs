using System;
using System.Threading.Tasks;
using ReactiveUI;
using ShareXMac.Core.Capture;
using SkiaSharp;

namespace ShareXMac.App.ViewModels;

/// <summary>
/// ViewModel for the region selector overlay. Tracks mouse position, drag state,
/// selection geometry, keyboard modifiers, and freeze mode. Invokes ICaptureService
/// for region and freeze captures.
/// </summary>
public sealed class RegionSelectorViewModel : ReactiveObject
{
    private readonly ICaptureService _captureService;

    private double _cursorX;
    private double _cursorY;
    private bool _isDragging;
    private double _selectionX;
    private double _selectionY;
    private double _selectionWidth;
    private double _selectionHeight;
    private bool _isFrozen;
    private bool _isShiftHeld;
    private bool _isCtrlHeld;

    private double _dragStartX;
    private double _dragStartY;

    public RegionSelectorViewModel(ICaptureService captureService)
    {
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
    }

    // --- Observable properties for rendering ---

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

    public bool IsDragging
    {
        get => _isDragging;
        set => this.RaiseAndSetIfChanged(ref _isDragging, value);
    }

    public double SelectionX
    {
        get => _selectionX;
        set => this.RaiseAndSetIfChanged(ref _selectionX, value);
    }

    public double SelectionY
    {
        get => _selectionY;
        set => this.RaiseAndSetIfChanged(ref _selectionY, value);
    }

    public double SelectionWidth
    {
        get => _selectionWidth;
        set => this.RaiseAndSetIfChanged(ref _selectionWidth, value);
    }

    public double SelectionHeight
    {
        get => _selectionHeight;
        set => this.RaiseAndSetIfChanged(ref _selectionHeight, value);
    }

    public bool IsFrozen
    {
        get => _isFrozen;
        set => this.RaiseAndSetIfChanged(ref _isFrozen, value);
    }

    public bool IsShiftHeld
    {
        get => _isShiftHeld;
        set => this.RaiseAndSetIfChanged(ref _isShiftHeld, value);
    }

    public bool IsCtrlHeld
    {
        get => _isCtrlHeld;
        set => this.RaiseAndSetIfChanged(ref _isCtrlHeld, value);
    }

    // --- Result and events ---

    /// <summary>The capture result. Null if cancelled.</summary>
    public CaptureResult? Result { get; private set; }

    /// <summary>Fired when capture completes successfully.</summary>
    public event Action? CaptureCompleted;

    /// <summary>Fired when the user cancels the selection.</summary>
    public event Action? Cancelled;

    /// <summary>
    /// Screen bitmap used for magnifier rendering and freeze mode.
    /// Loaded on overlay open via CaptureFullScreenAsync; replaced on freeze toggle.
    /// </summary>
    public SKBitmap? ScreenBitmap { get; set; }

    // --- Initialization ---

    /// <summary>
    /// Captures the initial screen bitmap for magnifier rendering.
    /// Call this when the overlay opens.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var capture = await _captureService.CaptureFullScreenAsync();
            ScreenBitmap = SKBitmap.Decode(capture.PngBytes);
        }
        catch (Exception ex)
        {
            // If initial capture fails, magnifier will simply not render
            System.Diagnostics.Debug.WriteLine($"[RegionSelector] Failed to capture initial screen bitmap: {ex.Message}");
        }
    }

    // --- Mouse tracking ---

    public void StartDrag(double x, double y)
    {
        _dragStartX = x;
        _dragStartY = y;
        SelectionX = x;
        SelectionY = y;
        SelectionWidth = 0;
        SelectionHeight = 0;
        IsDragging = true;
    }

    public void UpdateDrag(double x, double y)
    {
        CursorX = x;
        CursorY = y;

        if (!IsDragging) return;

        double startX = _dragStartX;
        double startY = _dragStartY;

        double rawWidth = x - startX;
        double rawHeight = y - startY;

        if (IsCtrlHeld)
        {
            // Anchor from center: start point is center, extend in both directions
            double halfW = Math.Abs(rawWidth);
            double halfH = Math.Abs(rawHeight);

            if (IsShiftHeld)
            {
                // Square constraint with center anchor
                double side = Math.Min(halfW, halfH);
                halfW = side;
                halfH = side;
            }

            SelectionX = startX - halfW;
            SelectionY = startY - halfH;
            SelectionWidth = halfW * 2;
            SelectionHeight = halfH * 2;
        }
        else
        {
            double selX = Math.Min(startX, x);
            double selY = Math.Min(startY, y);
            double selW = Math.Abs(rawWidth);
            double selH = Math.Abs(rawHeight);

            if (IsShiftHeld)
            {
                // Constrain to square
                double side = Math.Min(selW, selH);
                selW = side;
                selH = side;
                // Keep the corner anchored at the drag start
                if (x < startX) selX = startX - side;
                if (y < startY) selY = startY - side;
            }

            SelectionX = selX;
            SelectionY = selY;
            SelectionWidth = selW;
            SelectionHeight = selH;
        }
    }

    public void UpdateCursorPosition(double x, double y)
    {
        CursorX = x;
        CursorY = y;
    }

    public void EndDrag()
    {
        IsDragging = false;
        if (SelectionWidth > 1 && SelectionHeight > 1)
        {
            _ = ConfirmSelectionAsync();
        }
    }

    // --- Actions ---

    public async Task ConfirmSelectionAsync()
    {
        if (SelectionWidth < 1 || SelectionHeight < 1) return;

        try
        {
            var rect = new CGRectCapture(SelectionX, SelectionY, SelectionWidth, SelectionHeight);
            Result = await _captureService.CaptureRegionAsync(rect);
            CaptureCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RegionSelector] Capture failed: {ex.Message}");
            Cancel();
        }
    }

    public void Cancel()
    {
        Result = null;
        ScreenBitmap?.Dispose();
        ScreenBitmap = null;
        Cancelled?.Invoke();
    }

    public async Task ToggleFreeze()
    {
        try
        {
            if (!IsFrozen)
            {
                var capture = await _captureService.CaptureFreezeAsync();
                ScreenBitmap?.Dispose();
                ScreenBitmap = SKBitmap.Decode(capture.PngBytes);
                IsFrozen = true;
            }
            else
            {
                // Unfreeze: recapture the live screen
                var capture = await _captureService.CaptureFullScreenAsync();
                ScreenBitmap?.Dispose();
                ScreenBitmap = SKBitmap.Decode(capture.PngBytes);
                IsFrozen = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RegionSelector] Freeze toggle failed: {ex.Message}");
        }
    }
}
