namespace ShareXMac.Core.History;

using ShareXMac.Core.Capture;

public interface ICaptureHistory
{
    /// <summary>Insert a new capture record. Generates thumbnail from CaptureResult.PngBytes.</summary>
    Task InsertAsync(CaptureResult result, string? filePath, CancellationToken ct = default);

    /// <summary>
    /// Query capture records. Pass null to omit a filter.
    /// Results are ordered by CapturedAt descending (newest first).
    /// </summary>
    Task<IReadOnlyList<CaptureRecord>> QueryAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? modeFilter = null,
        int limit = 200,
        CancellationToken ct = default);

    /// <summary>Delete a capture record by ID. Does not delete the file on disk.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Total count of capture records.</summary>
    Task<int> CountAsync(CancellationToken ct = default);
}
