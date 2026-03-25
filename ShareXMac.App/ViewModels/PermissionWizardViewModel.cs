using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ShareXMac.Core.Permissions;

namespace ShareXMac.App.ViewModels;

public sealed class PermissionWizardViewModel : ReactiveObject
{
    private readonly IPermissionManager _permissionManager;
    private readonly Action _onDone;

    private string _screenRecordingStatus = "Not checked";
    private string _accessibilityStatus = "Not checked";

    public PermissionWizardViewModel(IPermissionManager permissionManager, Action onDone)
    {
        _permissionManager = permissionManager;
        _onDone = onDone;

        GrantScreenRecordingCommand = ReactiveCommand.CreateFromTask(GrantScreenRecordingAsync);
        OpenAccessibilitySettingsCommand = ReactiveCommand.Create(OpenAccessibilitySettings);
        DoneCommand = ReactiveCommand.Create(_onDone);

        // Refresh status on construction so the labels show current state
        RefreshStatus();
    }

    public ICommand GrantScreenRecordingCommand { get; }
    public ICommand OpenAccessibilitySettingsCommand { get; }
    public ICommand DoneCommand { get; }

    public string ScreenRecordingStatus
    {
        get => _screenRecordingStatus;
        private set => this.RaiseAndSetIfChanged(ref _screenRecordingStatus, value);
    }

    public string AccessibilityStatus
    {
        get => _accessibilityStatus;
        private set => this.RaiseAndSetIfChanged(ref _accessibilityStatus, value);
    }

    private void RefreshStatus()
    {
        ScreenRecordingStatus = _permissionManager.CheckScreenRecordingPermission() switch
        {
            PermissionStatus.Granted => "Granted",
            PermissionStatus.Denied => "Not granted -- click button above",
            _ => "Not determined"
        };
        AccessibilityStatus = _permissionManager.CheckAccessibilityPermission() switch
        {
            PermissionStatus.Granted => "Granted",
            PermissionStatus.Denied => "Not granted -- open Settings",
            _ => "Not determined"
        };
    }

    private async Task GrantScreenRecordingAsync()
    {
        var result = await _permissionManager.RequestScreenRecordingPermissionAsync();
        ScreenRecordingStatus = result == PermissionStatus.Granted
            ? "Granted"
            : "Denied -- you can grant it later in System Settings > Privacy > Screen Recording";
    }

    private void OpenAccessibilitySettings()
    {
        _permissionManager.OpenAccessibilitySettings();
        // Re-check after a brief pause to update the status label
        Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ =>
            Avalonia.Threading.Dispatcher.UIThread.Post(RefreshStatus));
    }
}
