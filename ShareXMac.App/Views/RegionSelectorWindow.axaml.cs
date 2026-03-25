using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using ShareXMac.App.ViewModels;
using SkiaSharp;

namespace ShareXMac.App.Views;

public partial class RegionSelectorWindow : Window
{
    private RegionSelectorViewModel? _viewModel;

    public RegionSelectorWindow()
    {
        InitializeComponent();
    }

    public RegionSelectorViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            DataContext = value;

            if (value != null)
            {
                value.CaptureCompleted += OnCaptureCompleted;
                value.Cancelled += OnCancelled;
            }
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Cursor = new Cursor(StandardCursorType.Cross);

        // Initialize the screen bitmap for magnifier
        if (_viewModel != null)
        {
            _ = _viewModel.InitializeAsync();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CaptureCompleted -= OnCaptureCompleted;
            _viewModel.Cancelled -= OnCancelled;
        }
        base.OnClosed(e);
    }

    // --- Pointer event handlers ---

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);
        _viewModel?.StartDrag(pos.X, pos.Y);
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);

        if (_viewModel != null)
        {
            if (_viewModel.IsDragging)
            {
                _viewModel.UpdateDrag(pos.X, pos.Y);
            }
            else
            {
                _viewModel.UpdateCursorPosition(pos.X, pos.Y);
            }
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _viewModel?.EndDrag();
    }

    // --- Keyboard event handlers ---

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_viewModel == null) return;

        switch (e.Key)
        {
            case Key.Escape:
                _viewModel.Cancel();
                e.Handled = true;
                break;
            case Key.Enter:
                _ = _viewModel.ConfirmSelectionAsync();
                e.Handled = true;
                break;
            case Key.Space:
                _ = _viewModel.ToggleFreeze();
                e.Handled = true;
                break;
            case Key.LeftShift:
            case Key.RightShift:
                _viewModel.IsShiftHeld = true;
                e.Handled = true;
                break;
            case Key.LeftCtrl:
            case Key.RightCtrl:
            case Key.LWin:
            case Key.RWin:
                _viewModel.IsCtrlHeld = true;
                e.Handled = true;
                break;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (_viewModel == null) return;

        switch (e.Key)
        {
            case Key.LeftShift:
            case Key.RightShift:
                _viewModel.IsShiftHeld = false;
                e.Handled = true;
                break;
            case Key.LeftCtrl:
            case Key.RightCtrl:
            case Key.LWin:
            case Key.RWin:
                _viewModel.IsCtrlHeld = false;
                e.Handled = true;
                break;
        }
    }

    // --- Custom SkiaSharp rendering ---

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context.Custom(new RegionSelectorDrawOperation(bounds, _viewModel));
    }

    private void OnCaptureCompleted()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Close());
    }

    private void OnCancelled()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Close());
    }
}

/// <summary>
/// SkiaSharp custom draw operation for the region selector overlay.
/// Draws screen dim, crosshair, selection rectangle, dimension label, magnifier, and freeze indicator.
/// </summary>
internal sealed class RegionSelectorDrawOperation : ICustomDrawOperation
{
    private readonly Rect _bounds;
    private readonly RegionSelectorViewModel? _vm;

    // Overlay colors
    private static readonly SKColor DimColor = new(0, 0, 0, 128);          // #80000000 - 50% black
    private static readonly SKColor SelectionBorder = new(0x3E, 0x83, 0xF2, 0xFF);  // #3E83F2 accent
    private static readonly SKColor SelectionFill = new(0x3E, 0x83, 0xF2, 0x1A);    // 10% accent
    private static readonly SKColor LabelBg = new(0x00, 0x00, 0x00, 0xCC);          // #CC000000
    private static readonly SKColor LabelText = SKColors.White;
    private static readonly SKColor CrosshairWhite = SKColors.White;
    private static readonly SKColor CrosshairShadow = SKColors.Black;
    private static readonly SKColor MagnifierBorder = SKColors.White;
    private static readonly SKColor MagnifierCrosshair = SKColors.Red;
    private static readonly SKColor MagnifierGrid = new(0xFF, 0xFF, 0xFF, 0x40);     // #40FFFFFF

    public RegionSelectorDrawOperation(Rect bounds, RegionSelectorViewModel? viewModel)
    {
        _bounds = bounds;
        _vm = viewModel;
    }

    public Rect Bounds => _bounds;

    public void Dispose() { }

    public bool Equals(ICustomDrawOperation? other)
        => other is RegionSelectorDrawOperation op && op._bounds == _bounds;

    public bool HitTest(Point p) => _bounds.Contains(p);

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
        if (leaseFeature == null) return;

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;

        if (_vm == null)
        {
            DrawDimOverlay(canvas, (float)_bounds.Width, (float)_bounds.Height);
            return;
        }

        float screenW = (float)_bounds.Width;
        float screenH = (float)_bounds.Height;
        float cursorX = (float)_vm.CursorX;
        float cursorY = (float)_vm.CursorY;

        bool hasSel = _vm.IsDragging || (_vm.SelectionWidth > 1 && _vm.SelectionHeight > 1);
        float selX = (float)_vm.SelectionX;
        float selY = (float)_vm.SelectionY;
        float selW = (float)_vm.SelectionWidth;
        float selH = (float)_vm.SelectionHeight;

        // (a) Screen dim with selection hole
        if (hasSel)
        {
            DrawDimWithSelectionHole(canvas, screenW, screenH, selX, selY, selW, selH);
        }
        else
        {
            DrawDimOverlay(canvas, screenW, screenH);
        }

        // (c) Selection rectangle border
        if (hasSel)
        {
            DrawSelectionRectangle(canvas, selX, selY, selW, selH);
        }

        // (d) Crosshair lines
        DrawCrosshair(canvas, screenW, screenH, cursorX, cursorY);

        // (e) Dimension label
        DrawDimensionLabel(canvas, screenW, screenH, cursorX, cursorY, hasSel, selX, selY, selW, selH);

        // (f) Magnifier loupe
        DrawMagnifier(canvas, screenW, screenH, cursorX, cursorY);

        // Freeze mode indicator
        if (_vm.IsFrozen)
        {
            DrawFreezeIndicator(canvas, screenW);
        }
    }

    private static void DrawDimOverlay(SKCanvas canvas, float width, float height)
    {
        using var paint = new SKPaint { Color = DimColor };
        canvas.DrawRect(0, 0, width, height, paint);
    }

    private static void DrawDimWithSelectionHole(SKCanvas canvas, float screenW, float screenH,
        float selX, float selY, float selW, float selH)
    {
        var selRect = new SKRect(selX, selY, selX + selW, selY + selH);

        // Draw dim around the selection using 4 rectangles
        using var dimPaint = new SKPaint { Color = DimColor };

        // Top
        canvas.DrawRect(0, 0, screenW, selY, dimPaint);
        // Bottom
        canvas.DrawRect(0, selY + selH, screenW, screenH - selY - selH, dimPaint);
        // Left
        canvas.DrawRect(0, selY, selX, selH, dimPaint);
        // Right
        canvas.DrawRect(selX + selW, selY, screenW - selX - selW, selH, dimPaint);

        // Selection fill (10% accent)
        using var fillPaint = new SKPaint { Color = SelectionFill };
        canvas.DrawRect(selRect, fillPaint);
    }

    private static void DrawSelectionRectangle(SKCanvas canvas, float selX, float selY, float selW, float selH)
    {
        using var borderPaint = new SKPaint
        {
            Color = SelectionBorder,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        canvas.DrawRect(selX, selY, selW, selH, borderPaint);
    }

    private static void DrawCrosshair(SKCanvas canvas, float screenW, float screenH, float cursorX, float cursorY)
    {
        // Black shadow offset (1,1)
        using var shadowPaint = new SKPaint
        {
            Color = CrosshairShadow,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false
        };
        canvas.DrawLine(cursorX + 1, 0, cursorX + 1, screenH, shadowPaint);
        canvas.DrawLine(0, cursorY + 1, screenW, cursorY + 1, shadowPaint);

        // White crosshair
        using var whitePaint = new SKPaint
        {
            Color = CrosshairWhite,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false
        };
        canvas.DrawLine(cursorX, 0, cursorX, screenH, whitePaint);
        canvas.DrawLine(0, cursorY, screenW, cursorY, whitePaint);
    }

    private static void DrawDimensionLabel(SKCanvas canvas, float screenW, float screenH,
        float cursorX, float cursorY, bool hasSel,
        float selX, float selY, float selW, float selH)
    {
        string text;
        float anchorX, anchorY;

        if (hasSel)
        {
            text = $"{(int)selW} x {(int)selH}";
            anchorX = selX + selW + 8;
            anchorY = selY + selH + 8;
        }
        else
        {
            text = $"X: {(int)cursorX}, Y: {(int)cursorY}";
            anchorX = cursorX + 8;
            anchorY = cursorY + 8;
        }

        using var textPaint = new SKPaint
        {
            Color = LabelText,
            TextSize = 12,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("SF Pro", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                ?? SKTypeface.Default
        };

        float textWidth = textPaint.MeasureText(text);
        float textHeight = 12f; // Approximate text height at 12px

        float padH = 6f;
        float padV = 2f;
        float pillWidth = textWidth + padH * 2;
        float pillHeight = textHeight + padV * 2;

        // Flip position if off-screen
        if (anchorX + pillWidth > screenW)
        {
            if (hasSel)
                anchorX = selX - pillWidth - 8;
            else
                anchorX = cursorX - pillWidth - 8;
        }
        if (anchorY + pillHeight > screenH)
        {
            if (hasSel)
                anchorY = selY - pillHeight - 8;
            else
                anchorY = cursorY - pillHeight - 8;
        }

        // Clamp to screen
        anchorX = Math.Max(0, anchorX);
        anchorY = Math.Max(0, anchorY);

        // Background pill
        using var bgPaint = new SKPaint { Color = LabelBg, IsAntialias = true };
        var pillRect = new SKRoundRect(new SKRect(anchorX, anchorY, anchorX + pillWidth, anchorY + pillHeight), 4f);
        canvas.DrawRoundRect(pillRect, bgPaint);

        // Text
        canvas.DrawText(text, anchorX + padH, anchorY + padV + textHeight, textPaint);
    }

    private void DrawMagnifier(SKCanvas canvas, float screenW, float screenH, float cursorX, float cursorY)
    {
        if (_vm?.ScreenBitmap == null) return;

        float radius = 50f; // 100px diameter
        float zoom = 5f;

        // Position: 16px above-right of cursor
        float magX = cursorX + 16 + radius;
        float magY = cursorY - 16 - radius;

        // Flip if near edges
        if (magX + radius > screenW)
            magX = cursorX - 16 - radius;
        if (magY - radius < 0)
            magY = cursorY + 16 + radius;

        // Clip to circle
        canvas.Save();
        var clipPath = new SKPath();
        clipPath.AddCircle(magX, magY, radius);
        canvas.ClipPath(clipPath);

        // Draw zoomed portion of screen bitmap
        var bitmap = _vm.ScreenBitmap;
        float srcSize = radius * 2 / zoom;

        // Map cursor position to bitmap coordinates
        float bitmapScaleX = bitmap.Width / screenW;
        float bitmapScaleY = bitmap.Height / screenH;
        float srcCenterX = cursorX * bitmapScaleX;
        float srcCenterY = cursorY * bitmapScaleY;
        float srcHalf = srcSize * bitmapScaleX / 2;

        var srcRect = new SKRect(
            srcCenterX - srcHalf,
            srcCenterY - srcHalf,
            srcCenterX + srcHalf,
            srcCenterY + srcHalf
        );
        var destRect = new SKRect(magX - radius, magY - radius, magX + radius, magY + radius);

        using var bitmapPaint = new SKPaint
        {
            FilterQuality = SKFilterQuality.None, // Nearest-neighbor for pixel-perfect zoom
            IsAntialias = false
        };
        canvas.DrawBitmap(bitmap, srcRect, destRect, bitmapPaint);

        // Pixel grid lines at 5x zoom
        float pixelSize = zoom;
        using var gridPaint = new SKPaint
        {
            Color = MagnifierGrid,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false
        };

        float left = magX - radius;
        float top = magY - radius;
        float right = magX + radius;
        float bottom = magY + radius;

        // Offset grid so lines align with pixel boundaries
        float offsetX = (cursorX * zoom) % pixelSize;
        float offsetY = (cursorY * zoom) % pixelSize;

        for (float gx = left + pixelSize - offsetX; gx < right; gx += pixelSize)
            canvas.DrawLine(gx, top, gx, bottom, gridPaint);
        for (float gy = top + pixelSize - offsetY; gy < bottom; gy += pixelSize)
            canvas.DrawLine(left, gy, right, gy, gridPaint);

        canvas.Restore();

        // Magnifier border (2px white circle)
        using var borderPaint = new SKPaint
        {
            Color = MagnifierBorder,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        canvas.DrawCircle(magX, magY, radius, borderPaint);

        // Red crosshair inside magnifier (20px arms)
        using var crossPaint = new SKPaint
        {
            Color = MagnifierCrosshair,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false
        };
        float armLength = 20f;
        canvas.DrawLine(magX - armLength / 2, magY, magX + armLength / 2, magY, crossPaint);
        canvas.DrawLine(magX, magY - armLength / 2, magX, magY + armLength / 2, crossPaint);

        clipPath.Dispose();
    }

    private static void DrawFreezeIndicator(SKCanvas canvas, float screenW)
    {
        const string text = "Screen Frozen";

        using var textPaint = new SKPaint
        {
            Color = LabelText,
            TextSize = 12,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("SF Pro", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                ?? SKTypeface.Default
        };

        float textWidth = textPaint.MeasureText(text);
        float textHeight = 12f;
        float padH = 6f;
        float padV = 2f;
        float pillWidth = textWidth + padH * 2;
        float pillHeight = textHeight + padV * 2;

        float pillX = (screenW - pillWidth) / 2;
        float pillY = 16f;

        using var bgPaint = new SKPaint { Color = LabelBg, IsAntialias = true };
        var pillRect = new SKRoundRect(new SKRect(pillX, pillY, pillX + pillWidth, pillY + pillHeight), 4f);
        canvas.DrawRoundRect(pillRect, bgPaint);

        canvas.DrawText(text, pillX + padH, pillY + padV + textHeight, textPaint);
    }
}
