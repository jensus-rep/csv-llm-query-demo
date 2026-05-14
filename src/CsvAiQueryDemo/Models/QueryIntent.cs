namespace CsvAiQueryDemo.Models;

public sealed class QueryIntent
{
    public required string Operation { get; init; }
    public string? Column { get; init; }
    public string? Operator { get; init; }
    public string? Value { get; init; }
    public string? GroupBy { get; init; }
    public int? Limit { get; init; }
}
