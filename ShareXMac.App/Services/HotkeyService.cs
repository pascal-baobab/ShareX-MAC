using System;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Data;

namespace ShareXMac.App.Services;

/// <summary>
/// Registers global hotkeys using SharpHook (CGEventTap / libuiohook).
/// Requires Input Monitoring permission (Accessibility) to fire globally.
///
/// Hotkey bindings:
///   Cmd+Shift+5 -> Capture Region (default, matches macOS convention)
///   Cmd+Shift+4 -> Capture Window
///   Cmd+Shift+3 -> Capture Full Screen
///
/// macOS 15.4 regression note: On some machines running macOS 15.4, global hotkeys
/// stop firing globally (only fire in-foreground). This is a confirmed Apple bug
/// affecting CGEventTap listeners. The NativeMenu capture items serve as the in-app
/// fallback -- do not remove the menu items even if hotkeys appear to work in testing.
/// See: https://developer.apple.com/forums/thread/744440
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private TaskPoolGlobalHook? _hook;
    private bool _disposed;
    private Action? _onCaptureRegion;
    private Action? _onCaptureWindow;
    private Action? _onCaptureFullScreen;

    /// <summary>
    /// Starts the global hook and registers capture action callbacks.
    /// All callbacks are invoked on the UI thread via Dispatcher.UIThread.Post
    /// because SharpHook fires events on a background thread, but Avalonia
    /// window operations require the UI thread.
    /// </summary>
    public void Start(Action onCaptureRegion, Action? onCaptureWindow = null, Action? onCaptureFullScreen = null)
    {
        _onCaptureRegion = onCaptureRegion;
        _onCaptureWindow = onCaptureWindow;
        _onCaptureFullScreen = onCaptureFullScreen;

        if (_hook != null) return;

        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;

        // Start the hook on a background thread to avoid blocking the UI thread.
        // SharpHook.TaskPoolGlobalHook processes events on the thread pool.
        _ = Task.Run(() => _hook.RunAsync());

        Console.WriteLine("[HotkeyService] Global hook started. Listening for Cmd+Shift+3/4/5.");
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        bool hasCmd = e.RawEvent.Mask.HasFlag(EventMask.LeftMeta);
        bool hasShift = e.RawEvent.Mask.HasFlag(EventMask.Shift);

        if (!hasCmd || !hasShift) return;

        // Cmd+Shift+5: Capture Region (default, matches macOS convention)
        if (e.Data.KeyCode == KeyCode.Vc5)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _onCaptureRegion?.Invoke());
        }
        // Cmd+Shift+4: Capture Window
        else if (e.Data.KeyCode == KeyCode.Vc4)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _onCaptureWindow?.Invoke());
        }
        // Cmd+Shift+3: Capture Full Screen
        else if (e.Data.KeyCode == KeyCode.Vc3)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _onCaptureFullScreen?.Invoke());
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _hook?.Dispose();
    }
}
