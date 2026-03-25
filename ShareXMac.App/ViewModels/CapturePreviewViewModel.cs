using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using ReactiveUI;
using ShareXMac.Core.Capture;

namespace ShareXMac.App.ViewModels;

/// <summary>
/// ViewModel for the floating capture preview popup.
/// Manages a 5-second auto-dismiss countdown (updated every 50ms for smooth progress bar),
/// hover-pause logic, and Copy/Save/Open action commands.
/// </summary>
public sealed class CapturePreviewViewModel : ReactiveObject, IDisposable
{
    private readonly CaptureResult _result;
    private readonly string? _filePath;
    private readonly CompositeDisposable _disposables = new();

    private double _progressPercent = 100.0;
    public double ProgressPercent
    {
        get => _progressPercent;
        set => this.RaiseAndSetIfChanged(ref _progressPercent, value);
    }

    private bool _isPaused;
    public bool IsPaused
    {
        get => _isPaused;
        set => this.RaiseAndSetIfChanged(ref _isPaused, value);
    }

    public Bitmap? ThumbnailBitmap { get; }

    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> DismissCommand { get; }

    /// <summary>Raised when the popup should close (countdown expired or user dismissed).</summary>
    public event Action? DismissRequested;

    public CapturePreviewViewModel(CaptureResult result, string? filePath)
    {
        _result = result;
        _filePath = filePath;

        // Create thumbnail bitmap from PNG bytes
        using var ms = new MemoryStream(result.PngBytes);
        ThumbnailBitmap = new Bitmap(ms);

        // Commands
        CopyCommand = ReactiveCommand.Create(OnCopy);
        SaveCommand = ReactiveCommand.Create(OnSave);
        OpenCommand = ReactiveCommand.Create(OnOpen);
        DismissCommand = ReactiveCommand.Create(OnDismiss);

        // 5-second auto-dismiss timer (updates every 50ms for smooth progress bar animation)
        const double totalMs = 5000.0;
        const int intervalMs = 50;
        var elapsed = 0.0;

        Observable.Interval(TimeSpan.FromMilliseconds(intervalMs))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                if (IsPaused) return;
                elapsed += intervalMs;
                ProgressPercent = Math.Max(0, 100.0 * (1.0 - elapsed / totalMs));
                if (elapsed >= totalMs)
                    DismissRequested?.Invoke();
            })
            .DisposeWith(_disposables);
    }

    private void OnCopy()
    {
        // Re-copy PNG bytes to clipboard.
        // Integration plan (02-05) will wire this through IOutputService.
        // For now this is a command target that the integration plan completes.
    }

    private void OnSave()
    {
        // Reveal file in Finder via macOS open -R command
        if (_filePath != null)
            Process.Start("open", $"-R \"{_filePath}\"");
    }

    private void OnOpen()
    {
        // Phase 4 stub: open file in Preview.app (annotation editor deferred)
        if (_filePath != null)
            Process.Start("open", $"\"{_filePath}\"");
    }

    private void OnDismiss() => DismissRequested?.Invoke();

    public void Dispose() => _disposables.Dispose();
}
