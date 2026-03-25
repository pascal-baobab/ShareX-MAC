using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using ShareXMac.App.ViewModels;
using ShareXMac.Core.Capture;
using SkiaSharp;

namespace ShareXMac.App.Views;

public partial class WindowPickerOverlay : Window
{
    private WindowPickerViewModel? _viewModel;

    public WindowPickerOverlay()
    {
        InitializeComponent();
    }

    public WindowPickerViewModel? ViewModel
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

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_viewModel != null)
        {
            _viewModel.ScreenHeight = Bounds.Height;
            await _viewModel.LoadWindowsAsync();
            InvalidateVisual();
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

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);
        _viewModel?.UpdateCursorPosition(pos.X, pos.Y);
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (_viewModel?.HoveredWindow != null)
        {
            _ = _viewModel.SelectWindow();
        }
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
            case Key.Tab:
                _viewModel.ToggleShadow();
                InvalidateVisual();
                e.Handled = true;
                break;
        }
    }

    // --- Custom SkiaSharp rendering ---

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context.Custom(new WindowPickerDrawOperation(bounds, _viewModel));
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
/// SkiaSharp custom draw operation for the window picker overlay.
/// Draws screen dim, window highlight, window name label, and shadow toggle indicator.
/// </summary>
internal sealed class WindowPickerDrawOperation : ICustomDrawOperation
{
    private readonly Rect _bounds;
    private readonly WindowPickerViewModel? _vm;

    // Colors matching UI spec
    private static readonly SKColor DimColor = new(0, 0, 0, 128);              // #80000000 - 50% black
    private static readonly SKColor WindowHighlight = new(0x3E, 0x83, 0xF2, 0x4D); // #4D3E83F2 - 30% accent blue
    private static readonly SKColor LabelBg = new(0x00, 0x00, 0x00, 0xCC);     // #CC000000
    private static readonly SKColor LabelText = SKColors.White;

    public WindowPickerDrawOperation(Rect bounds, WindowPickerViewModel? viewModel)
    {
        _bounds = bounds;
        _vm = viewModel;
    }

    public Rect Bounds => _bounds;

    public void Dispose() { }

    public bool Equals(ICustomDrawOperation? other)
        => other is WindowPickerDrawOperation op && op._bounds == _bounds;

    public bool HitTest(Point p) => _bounds.Contains(p);

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
        if (leaseFeature == null) return;

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;

        float screenW = (float)_bounds.Width;
        float screenH = (float)_bounds.Height;

        // (1) Screen dim - 50% black
        using var dimPaint = new SKPaint { Color = DimColor };
        canvas.DrawRect(0, 0, screenW, screenH, dimPaint);

        if (_vm == null) return;

        // (2) Window highlight for hovered window
        var hovered = _vm.HoveredWindow;
        if (hovered != null)
        {
            var (wx, wy, ww, wh) = _vm.GetWindowBounds(hovered);

            // Clear the dim over the hovered window and draw highlight
            using var clearPaint = new SKPaint
            {
                Color = SKColors.Transparent,
                BlendMode = SKBlendMode.Src
            };
            var windowRect = new SKRect((float)wx, (float)wy, (float)(wx + ww), (float)(wy + wh));

            canvas.Save();
            canvas.ClipRect(windowRect);
            canvas.DrawRect(windowRect, clearPaint);
            canvas.Restore();

            // Draw 30% accent blue highlight
            using var highlightPaint = new SKPaint
            {
                Color = WindowHighlight,
                IsAntialias = true
            };
            canvas.DrawRect(windowRect, highlightPaint);

            // (3) Window name label
            DrawWindowLabel(canvas, hovered, (float)wx, (float)wy, (float)ww);
        }

        // (4) Shadow toggle indicator in bottom-left corner
        DrawShadowIndicator(canvas, screenH);
    }

    private static void DrawWindowLabel(SKCanvas canvas, WindowInfo window, float wx, float wy, float ww)
    {
        string label = string.IsNullOrEmpty(window.WindowName)
            ? window.OwnerName
            : $"{window.OwnerName} - {window.WindowName}";

        using var textPaint = new SKPaint
        {
            Color = LabelText,
            TextSize = 12,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("SF Pro", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                ?? SKTypeface.Default
        };

        float textWidth = textPaint.MeasureText(label);
        float textHeight = 12f;
        float padH = 6f;
        float padV = 2f;
        float pillWidth = textWidth + padH * 2;
        float pillHeight = textHeight + padV * 2;

        // Center horizontally within window bounds, 8px below top edge
        float pillX = wx + (ww - pillWidth) / 2;
        float pillY = wy + 8;

        // Clamp to screen
        pillX = Math.Max(0, pillX);
        pillY = Math.Max(0, pillY);

        // Background pill
        using var bgPaint = new SKPaint { Color = LabelBg, IsAntialias = true };
        var pillRect = new SKRoundRect(new SKRect(pillX, pillY, pillX + pillWidth, pillY + pillHeight), 4f);
        canvas.DrawRoundRect(pillRect, bgPaint);

        // Text
        canvas.DrawText(label, pillX + padH, pillY + padV + textHeight, textPaint);
    }

    private void DrawShadowIndicator(SKCanvas canvas, float screenH)
    {
        if (_vm == null) return;

        string text = _vm.IncludeShadow ? "Shadow: On" : "Shadow: Off";

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

        // Bottom-left corner
        float pillX = 16;
        float pillY = screenH - pillHeight - 16;

        using var bgPaint = new SKPaint { Color = LabelBg, IsAntialias = true };
        var pillRect = new SKRoundRect(new SKRect(pillX, pillY, pillX + pillWidth, pillY + pillHeight), 4f);
        canvas.DrawRoundRect(pillRect, bgPaint);

        canvas.DrawText(text, pillX + padH, pillY + padV + textHeight, textPaint);
    }
}
