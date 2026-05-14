using CsvAiQueryDemo.Models;

namespace CsvAiQueryDemo.Services;

public static class PipelineSteps
{
    public static List<PipelineStep> CreateInitial()
    {
        return
        [
            Create("CSV lokal geladen"),
            Create("Dataset Profile erzeugt"),
            Create("Nutzerfrage empfangen"),
            Create("QueryIntent durch LLM erzeugt"),
            Create("QueryEngine in C# ausgeführt"),
            Create("Ergebnis zurückgegeben")
        ];
    }

    public static void MarkRunning(List<PipelineStep> steps, string name, string description)
    {
        Update(steps, name, "running", description);
    }

    public static void MarkSuccess(List<PipelineStep> steps, string name, string description)
    {
        Update(steps, name, "success", description);
    }

    public static void MarkError(List<PipelineStep> steps, string name, string description)
    {
        Update(steps, name, "error", description);
    }

    private static PipelineStep Create(string name)
    {
        return new PipelineStep { Name = name, Status = "pending", Description = string.Empty };
    }

    private static void Update(List<PipelineStep> steps, string name, string status, string description)
    {
        var step = steps.First(step => step.Name == name);
        step.Status = status;
        step.Description = description;
    }
}
