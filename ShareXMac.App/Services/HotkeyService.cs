using System;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Native;

namespace ShareXMac.App.Services;

/// <summary>
/// Registers global hotkeys using SharpHook (CGEventTap / libuiohook).
/// Requires Input Monitoring permission (Accessibility) to fire globally.
///
/// Phase 1: Logs a message when the hotkey fires (no capture yet).
/// Phase 2: Will call CaptureService.TakeSingleScreenshotAsync().
///
/// macOS 15.4 regression note: On some machines running macOS 15.4, global hotkeys
/// stop firing globally (only fire in-foreground). The NativeMenu "Capture Screen"
/// item is the in-app fallback. Do not remove the menu item.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private TaskPoolGlobalHook? _hook;
    private bool _disposed;

    public void Start()
    {
        if (_hook != null) return;

        _hook = new TaskPoolGlobalHook();

        _hook.KeyPressed += OnKeyPressed;

        // Start the hook on a background thread to avoid blocking the UI thread.
        // SharpHook.TaskPoolGlobalHook processes events on the thread pool.
        _ = Task.Run(() => _hook.RunAsync());

        Console.WriteLine("[HotkeyService] Global hook started. Listening for Cmd+Shift+5.");
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        // Detect Cmd+Shift+5: SharpHook reports modifier keys as separate events.
        // Check if the '5' key is pressed while Command and Shift modifiers are held.
        // Note: KeyCode.Vc5 is the '5' key on the main keyboard.
        bool isCaptureBind =
            e.Data.KeyCode == KeyCode.Vc5 &&
            e.RawEvent.Mask.HasFlag(ModifierMask.LeftMeta) &&
            e.RawEvent.Mask.HasFlag(ModifierMask.Shift);

        if (isCaptureBind)
        {
            Console.WriteLine("[HotkeyService] Hotkey fired: CaptureScreen (Cmd+Shift+5)");
            // Phase 1: Log only. Phase 2 will wire: CaptureService.TakeSingleScreenshotAsync()
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _hook?.Dispose();
    }
}
