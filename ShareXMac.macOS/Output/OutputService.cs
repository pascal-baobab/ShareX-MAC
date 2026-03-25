using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ShareXMac.Core.Capture;
using ShareXMac.Core.History;
using ShareXMac.Core.Output;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ShareXMac.macOS.Output;

/// <summary>
/// Handles clipboard write (NSPasteboard), file save, and history insertion.
/// NSPasteboard P/Invoke: generalPasteboard -> clearContents -> setData:forType:
/// </summary>
public sealed class OutputService : IOutputService
{
    private readonly ICaptureHistory _history;
    private int _counter;

    // NSPasteboard P/Invoke
    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit", EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjcGetClass(string name);

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjcMsgSendWithPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation", EntryPoint = "objc_msgSend")]
    private static extern bool ObjcMsgSendBoolPtrPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation", EntryPoint = "objc_msgSend")]
    private static extern IntPtr NSData_dataWithBytes(IntPtr cls, IntPtr selector, IntPtr bytes, nuint length);

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr NSStringFromClass(IntPtr cls);

    // Create NSData from managed byte[]
    private static IntPtr CreateNSData(byte[] bytes)
    {
        IntPtr cls = ObjcGetClass("NSData");
        IntPtr sel = SelRegisterName("dataWithBytes:length:");
        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                return NSData_dataWithBytes(cls, sel, (IntPtr)ptr, (nuint)bytes.Length);
            }
        }
    }

    // Create NSString from managed string
    private static IntPtr CreateNSString(string s)
    {
        IntPtr cls = ObjcGetClass("NSString");
        IntPtr sel = SelRegisterName("stringWithUTF8String:");
        IntPtr utf8 = Marshal.StringToCoTaskMemUTF8(s);
        try
        {
            return ObjcMsgSendWithPtr(cls, sel, utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    public OutputService(ICaptureHistory history)
    {
        _history = history;
    }

    public async Task<string?> ProcessCaptureAsync(CaptureResult result, OutputOptions? options = null, CancellationToken ct = default)
    {
        options ??= OutputOptions.Default;

        string? filePath = null;

        // 1. Copy to clipboard
        if (options.CopyToClipboard)
        {
            await Task.Run(() => CopyToClipboard(result.PngBytes), ct);
        }

        // 2. Save to file
        if (options.SaveToFile)
        {
            filePath = BuildFilePath(options, result);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllBytesAsync(filePath, result.PngBytes, ct);
        }

        // 3. Insert into history (always)
        await _history.InsertAsync(result, filePath, ct);

        return filePath;
    }

    private static void CopyToClipboard(byte[] pngBytes)
    {
        // NSPasteboard.generalPasteboard
        IntPtr nsPbClass = ObjcGetClass("NSPasteboard");
        IntPtr generalPasteboardSel = SelRegisterName("generalPasteboard");
        IntPtr pasteboard = ObjcMsgSend(nsPbClass, generalPasteboardSel);

        // clearContents
        IntPtr clearSel = SelRegisterName("clearContents");
        ObjcMsgSend(pasteboard, clearSel);

        // NSData from PNG bytes
        IntPtr nsData = CreateNSData(pngBytes);

        // setData:forType: with "public.png" UTI
        IntPtr nsType = CreateNSString("public.png");
        IntPtr setDataSel = SelRegisterName("setData:forType:");
        ObjcMsgSendBoolPtrPtr(pasteboard, setDataSel, nsData, nsType);
    }

    private string BuildFilePath(OutputOptions options, CaptureResult result)
    {
        int counter = System.Threading.Interlocked.Increment(ref _counter);
        string name = options.FilenameTemplate;
        name = name.Replace("{date}", result.CapturedAt.ToString("yyyy-MM-dd"));
        name = name.Replace("{time}", result.CapturedAt.ToString("HHmmss"));
        name = name.Replace("{type}", result.Mode.ToString().ToLowerInvariant());
        name = name.Replace("{counter}", counter.ToString("D4"));
        // Strip any remaining unknown tokens
        name = Regex.Replace(name, @"\{[^}]+\}", "");
        return Path.Combine(options.SaveDirectory, name + ".png");
    }
}
