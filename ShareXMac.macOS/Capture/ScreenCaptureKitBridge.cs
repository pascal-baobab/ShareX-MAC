using System.Runtime.InteropServices;
using ShareXMac.Core.Capture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace ShareXMac.macOS.Capture;

/// <summary>
/// Full ICaptureService implementation using CGWindowListCreateImage,
/// CGWindowListCopyWindowInfo, and CGGetActiveDisplayList P/Invoke.
///
/// All capture methods follow the same pipeline:
///   CGWindowListCreateImage -> CGImageGetWidth/Height -> CGDataProviderCopyData
///   -> BGRA32 pixel data -> ImageSharp Image.LoadPixelData -> PNG encode.
///
/// No net9.0-macos TFM required — all APIs are plain C in CoreGraphics.framework
/// and CoreFoundation.framework.
/// </summary>
public sealed class ScreenCaptureKitBridge : ICaptureService
{
    #region Constants

    // CGWindowListOption flags
    private const uint kCGWindowListOptionOnScreenOnly = 0x01;
    private const uint kCGWindowListOptionIncludingWindow = 0x08;
    private const uint kCGWindowListExcludeDesktopElements = 0x10;

    // Window/image IDs
    private const uint kCGNullWindowID = 0;

    // CGWindowImageOption flags
    private const uint kCGWindowImageDefault = 0;
    private const uint kCGWindowImageBoundsIgnoreFraming = 0x01;

    #endregion

    #region P/Invoke — CoreGraphics

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGWindowListCreateImage(
        CGRect screenBounds,
        uint listOption,
        uint windowID,
        uint imageOption);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGImageGetWidth(IntPtr image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGImageGetHeight(IntPtr image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGImageGetDataProvider(IntPtr image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGDataProviderCopyData(IntPtr provider);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGImageRelease(IntPtr image);

    /// <summary>Returns the list of active display IDs.</summary>
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern int CGGetActiveDisplayList(
        uint maxDisplays,
        [Out] uint[] activeDisplays,
        out uint displayCount);

    /// <summary>Returns the bounding rectangle (in global Cocoa coordinates) for a display.</summary>
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern CGRect CGDisplayBounds(uint display);

    /// <summary>Returns the height in pixels of the specified display.</summary>
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGDisplayPixelsHigh(uint display);

    #endregion

    #region P/Invoke — CoreFoundation (Data)

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFDataGetBytePtr(IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFDataGetLength(IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr obj);

    #endregion

    #region P/Invoke — CoreGraphics (Window List)

    /// <summary>
    /// Returns a CFArrayRef of CFDictionary entries describing on-screen windows.
    /// Caller must CFRelease the returned array.
    /// </summary>
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

    #endregion

    #region P/Invoke — CoreFoundation (CFArray / CFDictionary / CFNumber / CFString)

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFArrayGetCount(IntPtr array);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFDictionaryGetValue(IntPtr dict, IntPtr key);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFNumberGetValue(IntPtr number, int theType, out int value);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", EntryPoint = "CFNumberGetValue")]
    private static extern bool CFNumberGetDoubleValue(IntPtr number, int theType, out double value);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string cStr, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringGetCStringPtr(IntPtr theString, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFStringGetCString(IntPtr theString, IntPtr buffer, nint bufferSize, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFStringGetLength(IntPtr theString);

    // CFNumber type constants
    private const int kCFNumberSInt32Type = 3;
    private const int kCFNumberFloat64Type = 13;

    // CFString encoding
    private const uint kCFStringEncodingUTF8 = 0x08000100;

    // Cached CFString keys for CGWindowListCopyWindowInfo dictionary lookups.
    // Created once and reused — they are global constants that do not need release.
    private static readonly Lazy<IntPtr> s_kCGWindowNumber = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "kCGWindowNumber", kCFStringEncodingUTF8));
    private static readonly Lazy<IntPtr> s_kCGWindowOwnerName = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "kCGWindowOwnerName", kCFStringEncodingUTF8));
    private static readonly Lazy<IntPtr> s_kCGWindowName = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "kCGWindowName", kCFStringEncodingUTF8));
    private static readonly Lazy<IntPtr> s_kCGWindowBounds = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "kCGWindowBounds", kCFStringEncodingUTF8));
    private static readonly Lazy<IntPtr> s_kCGWindowLayer = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "kCGWindowLayer", kCFStringEncodingUTF8));

    // Sub-keys within the kCGWindowBounds sub-dictionary
    private static readonly Lazy<IntPtr> s_boundsX = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "X", kCFStringEncodingUTF8));
    private static readonly Lazy<IntPtr> s_boundsY = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "Y", kCFStringEncodingUTF8));
    private static readonly Lazy<IntPtr> s_boundsWidth = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "Width", kCFStringEncodingUTF8));
    private static readonly Lazy<IntPtr> s_boundsHeight = new(() =>
        CFStringCreateWithCString(IntPtr.Zero, "Height", kCFStringEncodingUTF8));

    #endregion

    #region CGRect struct

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public double X, Y, Width, Height;

        /// <summary>
        /// CGRectNull — all zeros. When passed to CGWindowListCreateImage,
        /// captures the entire virtual screen (all monitors).
        /// </summary>
        public static readonly CGRect Null = new() { X = 0, Y = 0, Width = 0, Height = 0 };
    }

    #endregion

    #region ICaptureService — Screen Capture Methods

    /// <inheritdoc />
    public async Task<CaptureResult> CaptureFullScreenAsync(uint displayId = 0, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            // Get the bounds for the requested display so we capture only that monitor.
            CGRect bounds = GetDisplayBounds(displayId);
            return CaptureImageToResult(
                bounds,
                kCGWindowListOptionOnScreenOnly,
                kCGNullWindowID,
                kCGWindowImageDefault,
                CaptureMode.FullScreen,
                displayId);
        }, ct);
    }

    /// <inheritdoc />
    public async Task<CaptureResult> CaptureAllScreensAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            // CGRect.Null = entire virtual screen across all connected monitors.
            return CaptureImageToResult(
                CGRect.Null,
                kCGWindowListOptionOnScreenOnly,
                kCGNullWindowID,
                kCGWindowImageDefault,
                CaptureMode.AllScreens,
                displayId: 0);
        }, ct);
    }

    /// <inheritdoc />
    public async Task<CaptureResult> CaptureRegionAsync(CGRectCapture rect, uint displayId = 0, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            // The rect parameter uses Avalonia convention (origin top-left, y increases downward).
            // CGWindowListCreateImage uses Cocoa global coordinates (origin bottom-left of primary
            // display, y increases upward). Convert:
            //   cocoaY = primaryDisplayHeight - rect.Y - rect.Height
            double primaryDisplayHeight = GetPrimaryDisplayHeight();
            double cocoaY = primaryDisplayHeight - rect.Y - rect.Height;

            var bounds = new CGRect
            {
                X = rect.X,
                Y = cocoaY,
                Width = rect.Width,
                Height = rect.Height
            };

            return CaptureImageToResult(
                bounds,
                kCGWindowListOptionOnScreenOnly,
                kCGNullWindowID,
                kCGWindowImageDefault,
                CaptureMode.Region,
                displayId);
        }, ct);
    }

    /// <inheritdoc />
    public async Task<CaptureResult> CaptureFreezeAsync(uint displayId = 0, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            // Identical to fullscreen capture but returns CaptureMode.Freeze.
            // The caller displays this bitmap as a fullscreen overlay for region selection.
            CGRect bounds = GetDisplayBounds(displayId);
            return CaptureImageToResult(
                bounds,
                kCGWindowListOptionOnScreenOnly,
                kCGNullWindowID,
                kCGWindowImageDefault,
                CaptureMode.Freeze,
                displayId);
        }, ct);
    }

    /// <inheritdoc />
    public async Task<CaptureResult> CaptureWindowAsync(uint windowId, bool includeShadow = true, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            // Use kCGWindowListOptionIncludingWindow to capture a specific window.
            // CGRect.Null lets CoreGraphics compute the window's own bounds.
            uint imageOption = includeShadow
                ? kCGWindowImageDefault
                : kCGWindowImageBoundsIgnoreFraming;

            return CaptureImageToResult(
                CGRect.Null,
                kCGWindowListOptionIncludingWindow,
                windowId,
                imageOption,
                CaptureMode.Window,
                displayId: 0);
        }, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WindowInfo>> GetWindowListAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            // Enumerate on-screen windows, excluding desktop elements (Dock, menu bar, etc.)
            IntPtr cfArray = CGWindowListCopyWindowInfo(
                kCGWindowListOptionOnScreenOnly | kCGWindowListExcludeDesktopElements,
                kCGNullWindowID);

            if (cfArray == IntPtr.Zero)
                throw new CaptureException("CGWindowListCopyWindowInfo returned null.");

            try
            {
                nint count = CFArrayGetCount(cfArray);
                var windows = new List<WindowInfo>();

                for (nint i = 0; i < count; i++)
                {
                    IntPtr dict = CFArrayGetValueAtIndex(cfArray, i);
                    if (dict == IntPtr.Zero) continue;

                    // Extract layer — filter to normal windows (layer == 0)
                    int? layer = GetIntFromDict(dict, s_kCGWindowLayer.Value);
                    if (layer is null || layer.Value != 0) continue;

                    // Extract window ID
                    int? windowNumber = GetIntFromDict(dict, s_kCGWindowNumber.Value);
                    if (windowNumber is null) continue;

                    // Extract owner name (required)
                    string? ownerName = GetStringFromDict(dict, s_kCGWindowOwnerName.Value);
                    if (string.IsNullOrEmpty(ownerName)) continue;

                    // Extract window name (optional)
                    string? windowName = GetStringFromDict(dict, s_kCGWindowName.Value);

                    // Extract bounds sub-dictionary
                    IntPtr boundsDict = CFDictionaryGetValue(dict, s_kCGWindowBounds.Value);
                    double x = 0, y = 0, w = 0, h = 0;
                    if (boundsDict != IntPtr.Zero)
                    {
                        x = GetDoubleFromDict(boundsDict, s_boundsX.Value) ?? 0;
                        y = GetDoubleFromDict(boundsDict, s_boundsY.Value) ?? 0;
                        w = GetDoubleFromDict(boundsDict, s_boundsWidth.Value) ?? 0;
                        h = GetDoubleFromDict(boundsDict, s_boundsHeight.Value) ?? 0;
                    }

                    windows.Add(new WindowInfo(
                        WindowId: (uint)windowNumber.Value,
                        OwnerName: ownerName,
                        WindowName: windowName,
                        X: x, Y: y, Width: w, Height: h,
                        Layer: layer.Value));
                }

                // List is already front-to-back (the order CGWindowListCopyWindowInfo returns)
                return (IReadOnlyList<WindowInfo>)windows.AsReadOnly();
            }
            finally
            {
                CFRelease(cfArray);
            }
        }, ct);
    }

    #endregion

    #region ICaptureService — Display Enumeration

    /// <inheritdoc />
    public uint[] GetActiveDisplayIds()
    {
        uint[] displays = new uint[16]; // 16 displays is a reasonable maximum
        int status = CGGetActiveDisplayList(16, displays, out uint count);
        if (status != 0)
            throw new CaptureException($"CGGetActiveDisplayList failed with status {status}.");
        return displays[..(int)count];
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Core capture pipeline shared by all capture methods.
    /// CGWindowListCreateImage -> pixel extraction -> ImageSharp BGRA32 -> PNG.
    /// </summary>
    private CaptureResult CaptureImageToResult(
        CGRect bounds,
        uint listOption,
        uint windowId,
        uint imageOption,
        CaptureMode mode,
        uint displayId)
    {
        IntPtr cgImage = CGWindowListCreateImage(bounds, listOption, windowId, imageOption);
        if (cgImage == IntPtr.Zero)
            throw new CaptureException(
                "CGWindowListCreateImage returned null. Screen Recording permission may not be granted.");

        try
        {
            int width = (int)CGImageGetWidth(cgImage);
            int height = (int)CGImageGetHeight(cgImage);

            IntPtr provider = CGImageGetDataProvider(cgImage);
            if (provider == IntPtr.Zero)
                throw new CaptureException("CGImage has no data provider.");

            IntPtr cfData = CGDataProviderCopyData(provider);
            if (cfData == IntPtr.Zero)
                throw new CaptureException("Failed to copy CGImage pixel data.");

            try
            {
                IntPtr pixelPtr = CFDataGetBytePtr(cfData);
                nint length = CFDataGetLength(cfData);

                byte[] rawPixels = new byte[length];
                Marshal.Copy(pixelPtr, rawPixels, 0, (int)length);

                // CGWindowListCreateImage returns BGRA pixel data
                using var image = Image.LoadPixelData<Bgra32>(rawPixels, width, height);
                using var ms = new MemoryStream();
                image.Save(ms, new PngEncoder());

                return new CaptureResult(
                    PngBytes: ms.ToArray(),
                    Mode: mode,
                    CapturedAt: DateTimeOffset.UtcNow,
                    Width: width,
                    Height: height,
                    DisplayId: displayId);
            }
            finally
            {
                CFRelease(cfData);
            }
        }
        finally
        {
            CGImageRelease(cgImage);
        }
    }

    /// <summary>
    /// Returns the CGRect bounds for the given display, or primary display if displayId is 0.
    /// </summary>
    private CGRect GetDisplayBounds(uint displayId)
    {
        if (displayId == 0)
        {
            // Get the primary display ID
            uint[] displays = new uint[1];
            int status = CGGetActiveDisplayList(1, displays, out uint count);
            if (status != 0 || count == 0)
                throw new CaptureException("Failed to get primary display ID.");
            displayId = displays[0];
        }

        return CGDisplayBounds(displayId);
    }

    /// <summary>
    /// Returns the height of the primary display in Cocoa global coordinates.
    /// Used for Avalonia top-left to Cocoa bottom-left coordinate conversion.
    /// </summary>
    private double GetPrimaryDisplayHeight()
    {
        uint[] displays = new uint[1];
        int status = CGGetActiveDisplayList(1, displays, out uint count);
        if (status != 0 || count == 0)
            throw new CaptureException("Failed to get primary display for coordinate conversion.");

        CGRect primaryBounds = CGDisplayBounds(displays[0]);
        return primaryBounds.Height;
    }

    #endregion

    #region CFDictionary Value Extraction Helpers

    /// <summary>Extracts an int value from a CFDictionary by key (CFNumber with kCFNumberSInt32Type).</summary>
    private static int? GetIntFromDict(IntPtr dict, IntPtr key)
    {
        IntPtr value = CFDictionaryGetValue(dict, key);
        if (value == IntPtr.Zero) return null;
        if (CFNumberGetValue(value, kCFNumberSInt32Type, out int result))
            return result;
        return null;
    }

    /// <summary>Extracts a double value from a CFDictionary by key (CFNumber with kCFNumberFloat64Type).</summary>
    private static double? GetDoubleFromDict(IntPtr dict, IntPtr key)
    {
        IntPtr value = CFDictionaryGetValue(dict, key);
        if (value == IntPtr.Zero) return null;
        if (CFNumberGetDoubleValue(value, kCFNumberFloat64Type, out double result))
            return result;
        return null;
    }

    /// <summary>
    /// Extracts a string value from a CFDictionary by key.
    /// Uses CFStringGetCStringPtr for fast path, falls back to CFStringGetCString.
    /// </summary>
    private static string? GetStringFromDict(IntPtr dict, IntPtr key)
    {
        IntPtr value = CFDictionaryGetValue(dict, key);
        if (value == IntPtr.Zero) return null;

        // Fast path: direct pointer to internal UTF-8 buffer
        IntPtr cStringPtr = CFStringGetCStringPtr(value, kCFStringEncodingUTF8);
        if (cStringPtr != IntPtr.Zero)
            return Marshal.PtrToStringUTF8(cStringPtr);

        // Slow path: copy to managed buffer
        nint length = CFStringGetLength(value);
        if (length <= 0) return null;

        // UTF-8 can be up to 4 bytes per character, plus null terminator
        nint bufferSize = length * 4 + 1;
        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            if (CFStringGetCString(value, buffer, bufferSize, kCFStringEncodingUTF8))
                return Marshal.PtrToStringUTF8(buffer);
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    #endregion
}
