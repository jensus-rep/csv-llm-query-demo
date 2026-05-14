namespace CsvAiQueryDemo.Models;

public sealed record ModelPromptInfo(
    string Provider,
    string SystemPrompt,
    string UserPrompt,
    string RequestPayload);
