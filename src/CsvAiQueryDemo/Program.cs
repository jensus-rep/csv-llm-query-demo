using CsvAiQueryDemo.Models;
using CsvAiQueryDemo.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

var dataPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "data", "demodaten.csv"));
var outputPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "output"));
var promptPath = Path.Combine(app.Environment.ContentRootPath, "Prompts", "query-intent-system-prompt.txt");
var explanationPromptPath = Path.Combine(app.Environment.ContentRootPath, "Prompts", "result-explanation-system-prompt.txt");
Directory.CreateDirectory(outputPath);

var csvLoader = new CsvLoader();
var rows = csvLoader.Load(dataPath, ';');
var datasetProfile = DatasetProfiler.CreateProfile("demo-contacts", Path.GetFileName(dataPath), rows, ';');

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(
    Path.Combine(outputPath, "dataset-profile.json"),
    JsonSerializer.Serialize(datasetProfile, jsonOptions));

var queryEngine = new QueryEngine(rows);
var queryIntentService = new QueryIntentService(new HttpClient(), promptPath, app.Configuration);
var resultExplanationService = new ResultExplanationService(new HttpClient(), explanationPromptPath, app.Configuration);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/dataset/profile", () => Results.Ok(datasetProfile));

app.MapPost("/api/chat", async (ChatRequest request, CancellationToken cancellationToken) =>
{
    var steps = PipelineSteps.CreateInitial();
    PipelineSteps.MarkSuccess(steps, "CSV lokal geladen", "CSV data is held locally in application memory.");
    PipelineSteps.MarkSuccess(steps, "Dataset Profile erzeugt", "DatasetProfile was generated from metadata and profiling values.");
    PipelineSteps.MarkSuccess(steps, "Nutzerfrage empfangen", "User question was received by the API.");

    try
    {
        PipelineSteps.MarkRunning(steps, "QueryIntent durch LLM erzeugt", "Creating a structured query intent.");
        var generation = await queryIntentService.CreateIntentAsync(request.Message, datasetProfile, cancellationToken);
        QueryIntentService.SaveIntent(generation.Intent, Path.Combine(outputPath, "query-intent.json"));
        PipelineSteps.MarkSuccess(steps, "QueryIntent durch LLM erzeugt", generation.Message);

        PipelineSteps.MarkRunning(steps, "QueryEngine in C# ausgeführt", "Executing the query locally in C#.");
        var queryResult = queryEngine.Execute(generation.Intent);
        QueryEngine.SaveResult(queryResult, Path.Combine(outputPath, "query-result.json"));
        PipelineSteps.MarkSuccess(steps, "QueryEngine in C# ausgeführt", queryResult.Message);

        var answer = await resultExplanationService.ExplainAsync(request.Message, queryResult, cancellationToken);
        if (generation.UsedFallback)
        {
            answer = $"{answer} Hinweis: OpenAI ist nicht konfiguriert; diese Demo-Frage wurde per lokalem Fallback in ein QueryIntent übersetzt.";
        }

        PipelineSteps.MarkSuccess(steps, "Ergebnis zurückgegeben", "The deterministic query result was returned to the frontend.");
        return Results.Ok(new ChatResponse(answer, generation.Intent, queryResult, steps));
    }
    catch (Exception ex)
    {
        PipelineSteps.MarkError(steps, "QueryIntent durch LLM erzeugt", ex.Message);
        var errorResult = new QueryResult
        {
            Operation = "unknown",
            Success = false,
            Result = null,
            Rows = [],
            Message = ex.Message,
            Source = "CsvAiQueryDemo"
        };

        return Results.BadRequest(new ChatResponse(ex.Message, null, errorResult, steps));
    }
});

app.MapGet("/api/output/query-intent", () => ReadOutputJson(outputPath, "query-intent.json"));
app.MapGet("/api/output/query-result", () => ReadOutputJson(outputPath, "query-result.json"));

app.Run();

static IResult ReadOutputJson(string outputPath, string fileName)
{
    var filePath = Path.Combine(outputPath, fileName);
    if (!File.Exists(filePath))
    {
        return Results.NotFound(new { message = $"{fileName} has not been written yet." });
    }

    return Results.Text(File.ReadAllText(filePath), "application/json");
}
