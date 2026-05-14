namespace CsvAiQueryDemo.Models;

public sealed class QueryResult
{
    public required string Operation { get; init; }
    public bool Success { get; init; }
    public object? Result { get; init; }
    public IReadOnlyList<Dictionary<string, string>> Rows { get; init; } = [];
    public required string Message { get; init; }
    public required string Source { get; init; }
}
