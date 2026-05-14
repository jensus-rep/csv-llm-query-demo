namespace CsvAiQueryDemo.Models;

public sealed class ColumnProfile
{
    public required string Name { get; init; }
    public required string InferredType { get; init; }
    public int EmptyCount { get; init; }
    public int NonEmptyCount { get; init; }
    public int UniqueCount { get; init; }
    public IReadOnlyList<string> ExampleValues { get; init; } = [];
    public IReadOnlyDictionary<string, int> TopValues { get; init; } = new Dictionary<string, int>();
}
