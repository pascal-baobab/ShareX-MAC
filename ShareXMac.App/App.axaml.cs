using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShareXMac.App.ViewModels;
using ShareXMac.App.Views;
using ShareXMac.Core.Capture;
using ShareXMac.Core.History;
using ShareXMac.Core.Output;
using ShareXMac.Core.Permissions;
using ShareXMac.macOS.Capture;
using ShareXMac.macOS.History;
using ShareXMac.macOS.Output;
using ShareXMac.macOS.Permissions;

namespace ShareXMac.App;

/// <summary>
/// Service container for the application. Poor man's DI -- no container needed at this scale.
/// </summary>
public sealed record AppServices(
    ICaptureService CaptureService,
    IOutputService OutputService,
    ICaptureHistory CaptureHistory,
    IPermissionManager PermissionManager
);

public partial class App : Application
{
    private static MainWindow? _mainWindow;

    /// <summary>Static accessor for application services. Initialized in OnFrameworkInitializationCompleted.</summary>
    public static AppServices? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // CRITICAL: Tray apps must use OnExplicitShutdown. The default OnLastWindowClose
            // kills the app when the permission wizard (or any window) is closed, because
            // the TrayIcon is not a window. Without this, closing the wizard = app exits.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Do NOT set desktop.MainWindow -- this is a menu bar app; the MainWindow
            // is optional and opened on demand from the menu. Setting it here would
            // show the window on every launch, which is wrong for a utility app.

            // --- Service construction (poor man's DI) ---
            var permissionManager = new TccPermissionManager();
            var captureHistory = new SqliteCaptureHistory();
            var captureService = new ScreenCaptureKitBridge();
            var outputService = new OutputService(captureHistory);

            Services = new AppServices(captureService, outputService, captureHistory, permissionManager);

            // --- AppViewModel with services ---
            var appViewModel = new AppViewModel(Services);
            DataContext = appViewModel;

            // --- HotkeyService wired to capture commands ---
            // SharpHook must start AFTER NSApplication is running (this callback fires after it is).
            var hotkeyService = new Services.HotkeyService();
            hotkeyService.Start(
                onCaptureRegion: () => appViewModel.CaptureRegionCommand.Execute(null),
                onCaptureWindow: () => appViewModel.CaptureWindowCommand.Execute(null),
                onCaptureFullScreen: () => appViewModel.CaptureFullScreenCommand.Execute(null)
            );

            // Check permissions on launch and show wizard if Screen Recording is not granted.
            CheckPermissionsOnStartup(permissionManager);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CheckPermissionsOnStartup(IPermissionManager permissionManager)
    {
        var screenStatus = permissionManager.CheckScreenRecordingPermission();
        if (screenStatus != PermissionStatus.Granted)
        {
            // Show the wizard on the UI thread after a brief delay to let the tray settle.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var wizard = new PermissionWizardWindow(permissionManager);
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
                DataContext = new MainWindowViewModel(Services!.CaptureHistory)
            };
        }
        return _mainWindow;
    }
}
