namespace CsvAiQueryDemo.Models;

public sealed class DatasetProfile
{
    public required string DatasetId { get; init; }
    public required string FileName { get; init; }
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public required string Delimiter { get; init; }
    public IReadOnlyList<ColumnProfile> Columns { get; init; } = [];
    public required string DataRef { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
