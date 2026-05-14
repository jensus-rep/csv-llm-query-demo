using CsvAiQueryDemo.Models;

namespace CsvAiQueryDemo.Services;

public sealed record QueryIntentGeneration(QueryIntent Intent, bool UsedFallback, string Message);
