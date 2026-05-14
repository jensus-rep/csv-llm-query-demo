using CsvAiQueryDemo.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var dataPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "data", "demodaten.csv"));
var outputPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "output"));
Directory.CreateDirectory(outputPath);

var csvLoader = new CsvLoader();
var rows = csvLoader.Load(dataPath, ';');
var datasetProfile = DatasetProfiler.CreateProfile("demo-contacts", Path.GetFileName(dataPath), rows, ';');

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(
    Path.Combine(outputPath, "dataset-profile.json"),
    JsonSerializer.Serialize(datasetProfile, jsonOptions));

app.MapGet("/", () => Results.Text("CsvAiQueryDemo is running."));

app.Run();
