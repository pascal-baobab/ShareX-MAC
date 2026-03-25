using System.IO;
using Avalonia.Media.Imaging;
using ShareXMac.Core.History;

namespace ShareXMac.App.ViewModels;

/// <summary>
/// Lightweight read-only wrapper around CaptureRecord for data-binding in HistoryItemCard.
/// Provides computed display properties (thumbnail bitmap, formatted date, filename).
/// </summary>
public sealed class HistoryItemViewModel
{
    public CaptureRecord Record { get; }

    public int Id => Record.Id;
    public string Mode => Record.Mode;
    public string? FilePath => Record.FilePath;
    public int Width => Record.Width;
    public int Height => Record.Height;

    /// <summary>Decoded Avalonia Bitmap from the JPEG thumbnail bytes stored in SQLite.</summary>
    public Bitmap? ThumbnailBitmap { get; }

    /// <summary>Filename only, or "Untitled" if no file path.</summary>
    public string DisplayFilename { get; }

    /// <summary>Formatted date string: "Mar 25, 2026 2:30 PM"</summary>
    public string DisplayDate { get; }

    public HistoryItemViewModel(CaptureRecord record)
    {
        Record = record;

        // Decode thumbnail from stored JPEG bytes
        if (record.ThumbnailBytes is { Length: > 0 })
        {
            using var ms = new MemoryStream(record.ThumbnailBytes);
            ThumbnailBitmap = new Bitmap(ms);
        }

        DisplayFilename = record.FilePath != null
            ? Path.GetFileName(record.FilePath)
            : "Untitled";

        DisplayDate = record.CapturedAt.LocalDateTime.ToString("MMM dd, yyyy h:mm tt");
    }
}
