using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ShareXMac.Core.Permissions;
using ShareXMac.macOS.Permissions;

namespace ShareXMac.App.ViewModels;

public sealed class AppViewModel : ReactiveObject
{
    private readonly IPermissionManager _permissionManager;

    public AppViewModel()
    {
        _permissionManager = new TccPermissionManager();

        CaptureScreenCommand = ReactiveCommand.CreateFromTask(OnCaptureScreenAsync);
        OpenSettingsCommand = ReactiveCommand.Create(OnOpenSettings);
        QuitCommand = ReactiveCommand.Create(OnQuit);
    }

    public ICommand CaptureScreenCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand QuitCommand { get; }

    private async Task OnCaptureScreenAsync()
    {
        var status = _permissionManager.CheckScreenRecordingPermission();
        if (status != PermissionStatus.Granted)
        {
            // Permission not granted -- open the wizard instead of attempting capture
            ShowPermissionWizard();
            return;
        }
        // Phase 1 placeholder: actual capture wired in Phase 2
        Console.WriteLine("[ShareXMac] Capture triggered from menu. Phase 1 placeholder.");
        await Task.CompletedTask;
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
        var wizard = new Views.PermissionWizardWindow(_permissionManager);
        wizard.Show();
    }
}
