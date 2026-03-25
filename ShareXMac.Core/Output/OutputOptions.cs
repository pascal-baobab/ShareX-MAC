namespace ShareXMac.Core.Output;

/// <summary>
/// Configuration for what happens after a successful capture.
/// Defaults match the user decisions from Phase 2 CONTEXT.md.
/// </summary>
public sealed record OutputOptions
{
    /// <summary>Copy PNG to macOS clipboard via NSPasteboard. Default: true.</summary>
    public bool CopyToClipboard { get; init; } = true;

    /// <summary>Save PNG to local file. Default: true.</summary>
    public bool SaveToFile { get; init; } = true;

    /// <summary>
    /// Root directory for saved captures. Default: ~/Pictures/ShareXMac/
    /// </summary>
    public string SaveDirectory { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ShareXMac");

    /// <summary>
    /// Filename template. Supported tokens: {date}, {time}, {type}, {counter}.
    /// Example: "Screenshot_{date}_{time}" -> "Screenshot_2026-03-25_143022"
    /// </summary>
    public string FilenameTemplate { get; init; } = "Screenshot_{date}_{time}";

    public static readonly OutputOptions Default = new();
}
