using ScreenCaptureKit;
using CoreImage;
using Foundation;
using ShareXMac.Core.Capture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace ShareXMac.macOS.Capture;

/// <summary>
/// Captures a single full-screen screenshot using ScreenCaptureKit
/// (SCScreenshotManager.CaptureImageAsync).
///
/// IMPORTANT NSRunLoop requirement: This code requires NSApplication to be running
/// (Avalonia's UseMacOS() satisfies this). Calling any SCKit async method from a
/// thread without an active NSRunLoop causes an indefinite hang. See:
/// https://github.com/dotnet/macios/issues/17350
/// </summary>
public sealed class ScreenCaptureKitBridge : ICaptureService
{
    public async Task<byte[]> TakeSingleScreenshotAsync(CancellationToken ct = default)
    {
        // Step 1: Enumerate shareable content to get the primary display.
        // GetShareableContentAsync requires an active NSRunLoop — it will hang
        // indefinitely if NSApplication is not initialized.
        SCShareableContent content;
        try
        {
            content = await SCShareableContent.GetShareableContentAsync();
        }
        catch (Exception ex)
        {
            throw new CaptureException("Failed to enumerate shareable content. " +
                "Ensure Screen Recording permission is granted and NSApplication is initialized.", ex);
        }

        var displays = content.Displays;
        if (displays == null || displays.Length == 0)
            throw new CaptureException("No displays found via SCShareableContent.");

        // Use the first display (primary display) for the proof-of-concept screenshot.
        var primaryDisplay = displays[0];

        // Step 2: Build a content filter targeting the entire primary display.
        var filter = new SCContentFilter(primaryDisplay, content.Windows);

        // Step 3: Configure screenshot parameters.
        var config = new SCStreamConfiguration
        {
            Width = (nuint)primaryDisplay.Width,
            Height = (nuint)primaryDisplay.Height,
            PixelFormat = 0x42475241, // kCVPixelFormatType_32BGRA
            ShowsCursor = false
        };

        // Step 4: Capture the image via SCScreenshotManager.
        CGImage? cgImage;
        try
        {
            cgImage = await SCScreenshotManager.CaptureImageAsync(filter, config);
        }
        catch (Exception ex)
        {
            throw new CaptureException("SCScreenshotManager.CaptureImageAsync failed. " +
                "Screen Recording permission may have been denied.", ex);
        }

        if (cgImage == null)
            throw new CaptureException("SCScreenshotManager returned a null CGImage.");

        // Step 5: Encode CGImage to PNG bytes using ImageSharp.
        // We use ImageSharp (not System.Drawing) to avoid PlatformNotSupportedException on macOS.
        // CGImage -> raw BGRA bytes -> ImageSharp Image<Bgra32> -> PNG stream.
        int width = (int)cgImage.Width;
        int height = (int)cgImage.Height;
        var dataProvider = cgImage.DataProvider;
        if (dataProvider == null)
            throw new CaptureException("CGImage has no data provider.");

        using var nsData = dataProvider.CopyData();
        if (nsData == null)
            throw new CaptureException("Failed to copy CGImage pixel data.");

        byte[] rawPixels = nsData.ToArray();

        using var image = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Bgra32>(
            rawPixels, width, height);

        using var ms = new System.IO.MemoryStream();
        await image.SaveAsync(ms, new PngEncoder(), ct);
        return ms.ToArray();
    }
}
