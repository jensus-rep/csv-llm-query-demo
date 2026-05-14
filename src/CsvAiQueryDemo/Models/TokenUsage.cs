namespace CsvAiQueryDemo.Models;

public sealed record TokenUsage(int InputTokens, int OutputTokens, int TotalTokens)
{
    public static TokenUsage Zero { get; } = new(0, 0, 0);

    public TokenUsage Add(TokenUsage? other)
    {
        if (other is null)
        {
            return this;
        }

        return new TokenUsage(
            InputTokens + other.InputTokens,
            OutputTokens + other.OutputTokens,
            TotalTokens + other.TotalTokens);
    }
}
