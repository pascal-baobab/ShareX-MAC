using System.Runtime.InteropServices;
using ShareXMac.Core.Capture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace ShareXMac.macOS.Capture;

/// <summary>
/// Captures screenshots using CGWindowListCreateImage (P/Invoke).
/// Phase 1 established the full-screen path. Phase 2 Plan 02-02 will implement
/// all capture modes (region, window, freeze, all-screens).
///
/// CGWindowListCreateImage works without net9.0-macos TFM — it's a plain C API
/// in CoreGraphics.framework.
/// </summary>
public sealed class ScreenCaptureKitBridge : ICaptureService
{
    // CGWindowListCreateImage captures the screen as a CGImageRef.
    // CGRectNull (all zeros) = capture entire main display.
    // kCGWindowListOptionOnScreenOnly = 0, kCGNullWindowID = 0
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGWindowListCreateImage(
        CGRect screenBounds,
        uint listOption,     // kCGWindowListOptionAll = 0
        uint windowID,       // kCGNullWindowID = 0
        uint imageOption);   // kCGWindowImageDefault = 0

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGImageGetWidth(IntPtr image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGImageGetHeight(IntPtr image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGImageGetDataProvider(IntPtr image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGDataProviderCopyData(IntPtr provider);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFDataGetBytePtr(IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFDataGetLength(IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr obj);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGImageRelease(IntPtr image);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public double X, Y, Width, Height;
        public static readonly CGRect Null = new() { X = 0, Y = 0, Width = 0, Height = 0 };
    }

    public async Task<CaptureResult> CaptureFullScreenAsync(uint displayId = 0, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            // CGWindowListCreateImage with CGRectNull captures the entire main display.
            // kCGWindowListOptionOnScreenOnly = 0x01, kCGNullWindowID = 0, kCGWindowImageDefault = 0
            IntPtr cgImage = CGWindowListCreateImage(
                CGRect.Null,
                0x01,  // kCGWindowListOptionOnScreenOnly
                0,     // kCGNullWindowID
                0);    // kCGWindowImageDefault

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
                        Mode: CaptureMode.FullScreen,
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
        }, ct);
    }

    /// <summary>Stub — will be implemented in Plan 02-02.</summary>
    public Task<CaptureResult> CaptureAllScreensAsync(CancellationToken ct = default)
        => throw new NotImplementedException("CaptureAllScreensAsync will be implemented in Plan 02-02.");

    /// <summary>Stub — will be implemented in Plan 02-02.</summary>
    public Task<CaptureResult> CaptureRegionAsync(CGRectCapture rect, uint displayId = 0, CancellationToken ct = default)
        => throw new NotImplementedException("CaptureRegionAsync will be implemented in Plan 02-02.");

    /// <summary>Stub — will be implemented in Plan 02-02.</summary>
    public Task<CaptureResult> CaptureWindowAsync(uint windowId, bool includeShadow = true, CancellationToken ct = default)
        => throw new NotImplementedException("CaptureWindowAsync will be implemented in Plan 02-02.");

    /// <summary>Stub — will be implemented in Plan 02-02.</summary>
    public Task<CaptureResult> CaptureFreezeAsync(uint displayId = 0, CancellationToken ct = default)
        => throw new NotImplementedException("CaptureFreezeAsync will be implemented in Plan 02-02.");

    /// <summary>Stub — will be implemented in Plan 02-02.</summary>
    public Task<IReadOnlyList<WindowInfo>> GetWindowListAsync(CancellationToken ct = default)
        => throw new NotImplementedException("GetWindowListAsync will be implemented in Plan 02-02.");

    /// <summary>Stub — will be implemented in Plan 02-02.</summary>
    public uint[] GetActiveDisplayIds()
        => throw new NotImplementedException("GetActiveDisplayIds will be implemented in Plan 02-02.");
}
