using Avalonia;
using System;

namespace ShareXMac.App;

internal sealed class Program
{
    // Entry point. MUST NOT call any native macOS APIs before BuildAvaloniaApp().Start().
    // Avalonia's UseMacOS() calls NSApplication.Init() internally. Any SCKit or
    // SharpHook call before this point will hang indefinitely (NSRunLoop not running).
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
}
