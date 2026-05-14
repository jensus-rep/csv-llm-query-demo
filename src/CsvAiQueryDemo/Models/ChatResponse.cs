namespace CsvAiQueryDemo.Models;

public sealed record ChatResponse(
    string Answer,
    QueryIntent? QueryIntent,
    QueryResult QueryResult,
    IReadOnlyList<PipelineStep> PipelineSteps,
    DatasetProfile DatasetProfile,
    ModelPromptInfo? QueryIntentPrompt);
