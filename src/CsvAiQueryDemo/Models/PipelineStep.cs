namespace CsvAiQueryDemo.Models;

public sealed class PipelineStep
{
    public required string Name { get; init; }
    public required string Status { get; set; }
    public required string Description { get; set; }
}
