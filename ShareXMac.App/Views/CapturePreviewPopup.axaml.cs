using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ShareXMac.App.ViewModels;
using ShareXMac.Core.Capture;

namespace ShareXMac.App.Views;

/// <summary>
/// 280x80 floating borderless popup that appears in the bottom-right corner
/// after a screen capture. Shows thumbnail + Copy/Save/Open buttons with a
/// 5-second auto-dismiss countdown that pauses on hover.
/// </summary>
public partial class CapturePreviewPopup : Window
{
    public CapturePreviewPopup()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        PositionBottomRight();
    }

    /// <summary>
    /// Position the popup 16px from the bottom-right corner of the primary screen.
    /// </summary>
    private void PositionBottomRight()
    {
        var screen = Screens.Primary;
        if (screen == null) return;

        var workingArea = screen.WorkingArea;
        var scaling = screen.Scaling;

        // WorkingArea is in device pixels; Position expects device pixels
        var x = (int)(workingArea.Right - (280 * scaling) - (16 * scaling));
        var y = (int)(workingArea.Bottom - (80 * scaling) - (16 * scaling));
        Position = new PixelPoint(x, y);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (DataContext is CapturePreviewViewModel vm)
            vm.IsPaused = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (DataContext is CapturePreviewViewModel vm)
            vm.IsPaused = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Static factory: creates and shows a CapturePreviewPopup for the given capture result.
    /// </summary>
    public static CapturePreviewPopup Show(CaptureResult result, string? filePath)
    {
        var vm = new CapturePreviewViewModel(result, filePath);
        var popup = new CapturePreviewPopup { DataContext = vm };
        vm.DismissRequested += () => Dispatcher.UIThread.Post(() =>
        {
            vm.Dispose();
            popup.Close();
        });
        popup.Show();
        return popup;
    }
}
