using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShareXMac.App.ViewModels;
using ShareXMac.App.Views;
using ShareXMac.Core.Permissions;
using ShareXMac.macOS.Permissions;

namespace ShareXMac.App;

public partial class App : Application
{
    private static MainWindow? _mainWindow;
    private readonly IPermissionManager _permissionManager = new TccPermissionManager();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Do NOT set desktop.MainWindow -- this is a menu bar app; the MainWindow
            // is optional and opened on demand from the menu. Setting it here would
            // show the window on every launch, which is wrong for a utility app.

            // Initialize the HotkeyService after the framework is ready.
            // SharpHook must start AFTER NSApplication is running (this callback fires after it is).
            var hotkeyService = new Services.HotkeyService();
            hotkeyService.Start();

            // Check permissions on launch and show wizard if Screen Recording is not granted.
            CheckPermissionsOnStartup();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CheckPermissionsOnStartup()
    {
        var screenStatus = _permissionManager.CheckScreenRecordingPermission();
        if (screenStatus != PermissionStatus.Granted)
        {
            // Show the wizard on the UI thread after a brief delay to let the tray settle.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var wizard = new PermissionWizardWindow(_permissionManager);
                wizard.Show();
            }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
        }
    }

    public static MainWindow GetOrCreateMainWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsVisible)
        {
            _mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }
        return _mainWindow;
    }
}
