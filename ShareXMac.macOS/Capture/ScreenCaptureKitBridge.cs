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

    #region P/Invoke — CoreFoundation

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFDataGetBytePtr(IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFDataGetLength(IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr obj);

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
    public Task<IReadOnlyList<WindowInfo>> GetWindowListAsync(CancellationToken ct = default)
        => throw new NotImplementedException("GetWindowListAsync — will be completed in Task 2.");

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
}
