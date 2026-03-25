using System.Runtime.InteropServices;
using ShareXMac.Core.Capture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace ShareXMac.macOS.Capture;

/// <summary>
/// Captures a single full-screen screenshot using CGWindowListCreateImage (P/Invoke).
/// This is a Phase 1 proof-of-concept. Phase 2 will replace this with
/// ScreenCaptureKit via a thin native Swift dylib, as per the architecture decision.
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

    public async Task<byte[]> TakeSingleScreenshotAsync(CancellationToken ct = default)
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
                    return ms.ToArray();
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
}
