using SQLite;

namespace ShareXMac.Core.History;

/// <summary>
/// Row in the capture history SQLite table.
/// sqlite-net-pcl requires a parameterless public constructor and [Table]/[PrimaryKey] attributes.
/// </summary>
[Table("captures")]
public sealed class CaptureRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>UTC timestamp of the capture.</summary>
    public DateTimeOffset CapturedAt { get; set; }

    /// <summary>CaptureMode enum value as string for readability in the DB.</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>Full path to the saved PNG file. Null if SaveToFile was false.</summary>
    public string? FilePath { get; set; }

    /// <summary>Width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>
    /// JPEG thumbnail bytes (100x100 max, 80% quality) stored inline in SQLite.
    /// Small enough to display in history list without disk seeks.
    /// </summary>
    public byte[]? ThumbnailBytes { get; set; }
}
