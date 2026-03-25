using SQLite;
using ShareXMac.Core.Capture;
using ShareXMac.Core.History;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ShareXMac.macOS.History;

/// <summary>
/// SQLite implementation of ICaptureHistory using sqlite-net-pcl.
/// Database file: ~/Library/Application Support/ShareXMac/history.db
/// Thumbnail: 100x100 max JPEG stored inline (avoids extra disk reads in list view).
/// </summary>
public sealed class SqliteCaptureHistory : ICaptureHistory
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _db;

    public SqliteCaptureHistory()
    {
        string appSupport = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string dir = Path.Combine(appSupport, "ShareXMac");
        Directory.CreateDirectory(dir);
        _dbPath = Path.Combine(dir, "history.db");
    }

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db != null) return _db;
        _db = new SQLiteAsyncConnection(_dbPath);
        await _db.CreateTableAsync<CaptureRecord>();
        return _db;
    }

    public async Task InsertAsync(CaptureResult result, string? filePath, CancellationToken ct = default)
    {
        byte[] thumbnail = GenerateThumbnail(result.PngBytes);
        var record = new CaptureRecord
        {
            CapturedAt = result.CapturedAt,
            Mode = result.Mode.ToString(),
            FilePath = filePath,
            Width = result.Width,
            Height = result.Height,
            ThumbnailBytes = thumbnail
        };
        var db = await GetDbAsync();
        await db.InsertAsync(record);
    }

    public async Task<IReadOnlyList<CaptureRecord>> QueryAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? modeFilter = null,
        int limit = 200,
        CancellationToken ct = default)
    {
        var db = await GetDbAsync();
        var query = db.Table<CaptureRecord>().OrderByDescending(r => r.CapturedAt);

        // sqlite-net-pcl TableQuery filters
        if (from.HasValue)
            query = query.Where(r => r.CapturedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.CapturedAt <= to.Value);
        if (!string.IsNullOrEmpty(modeFilter))
            query = query.Where(r => r.Mode == modeFilter);

        var results = await query.Take(limit).ToListAsync();
        return results.AsReadOnly();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var db = await GetDbAsync();
        await db.DeleteAsync<CaptureRecord>(id);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        var db = await GetDbAsync();
        return await db.Table<CaptureRecord>().CountAsync();
    }

    private static byte[] GenerateThumbnail(byte[] pngBytes)
    {
        // Decode PNG, resize to max 100x100 maintaining aspect ratio, encode as JPEG 80%
        using var image = Image.Load(pngBytes);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(100, 100),
            Mode = ResizeMode.Max
        }));
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 80 });
        return ms.ToArray();
    }
}
