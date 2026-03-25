using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ShareXMac.Core.Permissions;
using ShareXMac.App.Views;

namespace ShareXMac.App.ViewModels;

public sealed class AppViewModel : ReactiveObject
{
    private readonly AppServices _services;
    private readonly IPermissionManager _permissionManager;

    public AppViewModel(AppServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _permissionManager = services.PermissionManager;

        CaptureRegionCommand = ReactiveCommand.CreateFromTask(OnCaptureRegionAsync);
        CaptureWindowCommand = ReactiveCommand.CreateFromTask(OnCaptureWindowAsync);
        CaptureFullScreenCommand = ReactiveCommand.CreateFromTask(OnCaptureFullScreenAsync);
        CaptureFreezeCommand = ReactiveCommand.CreateFromTask(OnCaptureFreezeAsync);
        OpenSettingsCommand = ReactiveCommand.Create(OnOpenSettings);
        QuitCommand = ReactiveCommand.Create(OnQuit);
    }

    public ICommand CaptureRegionCommand { get; }
    public ICommand CaptureWindowCommand { get; }
    public ICommand CaptureFullScreenCommand { get; }
    public ICommand CaptureFreezeCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand QuitCommand { get; }

    private async Task OnCaptureRegionAsync()
    {
        if (!CheckPermission()) return;

        var vm = new RegionSelectorViewModel(_services.CaptureService);
        var window = new RegionSelectorWindow { DataContext = vm };

        var tcs = new TaskCompletionSource<Core.Capture.CaptureResult?>();
        vm.CaptureCompleted += () => tcs.TrySetResult(vm.Result);
        vm.Cancelled += () => tcs.TrySetResult(null);

        window.Show();
        var result = await tcs.Task;
        window.Close();

        if (result != null)
            await ProcessCaptureResult(result);
    }

    private async Task OnCaptureWindowAsync()
    {
        if (!CheckPermission()) return;

        var vm = new WindowPickerViewModel(_services.CaptureService);
        var window = new WindowPickerOverlay { DataContext = vm };

        var tcs = new TaskCompletionSource<Core.Capture.CaptureResult?>();
        vm.CaptureCompleted += () => tcs.TrySetResult(vm.Result);
        vm.Cancelled += () => tcs.TrySetResult(null);

        window.Show();
        await vm.LoadWindowsAsync(); // Load window list after overlay is visible
        var result = await tcs.Task;
        window.Close();

        if (result != null)
            await ProcessCaptureResult(result);
    }

    private async Task OnCaptureFullScreenAsync()
    {
        if (!CheckPermission()) return;
        var result = await _services.CaptureService.CaptureFullScreenAsync();
        await ProcessCaptureResult(result);
    }

    private async Task OnCaptureFreezeAsync()
    {
        if (!CheckPermission()) return;

        // Freeze capture: take fullscreen, then open region selector over frozen image
        var vm = new RegionSelectorViewModel(_services.CaptureService);
        await vm.ToggleFreeze(); // Pre-freeze before showing overlay
        var window = new RegionSelectorWindow { DataContext = vm };

        var tcs = new TaskCompletionSource<Core.Capture.CaptureResult?>();
        vm.CaptureCompleted += () => tcs.TrySetResult(vm.Result);
        vm.Cancelled += () => tcs.TrySetResult(null);

        window.Show();
        var result = await tcs.Task;
        window.Close();

        if (result != null)
            await ProcessCaptureResult(result);
    }

    private bool CheckPermission()
    {
        var status = _permissionManager.CheckScreenRecordingPermission();
        if (status != PermissionStatus.Granted)
        {
            ShowPermissionWizard();
            return false;
        }
        return true;
    }

    /// <summary>Common post-capture pipeline: OutputService (clipboard + file + history) then preview popup.</summary>
    private async Task ProcessCaptureResult(Core.Capture.CaptureResult result)
    {
        try
        {
            string? filePath = await _services.OutputService.ProcessCaptureAsync(result);
            // Show preview popup on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CapturePreviewPopup.Show(result, filePath);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShareXMac] Capture output failed: {ex.Message}");
        }
    }

    private void OnOpenSettings()
    {
        // MainWindow is a singleton -- show it if hidden, create if not yet created
        var window = App.GetOrCreateMainWindow();
        window.Show();
        window.Activate();
    }

    private void OnQuit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }

    internal void ShowPermissionWizard()
    {
        var wizard = new PermissionWizardWindow(_permissionManager);
        wizard.Show();
    }
}
