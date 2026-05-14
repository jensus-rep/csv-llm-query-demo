using CsvAiQueryDemo.Models;
using CsvAiQueryDemo.Services;
using Microsoft.Extensions.Configuration;

namespace CsvAiQueryDemo.Tests;

public sealed class ResultExplanationServiceTests
{
    [Fact]
    public void BuildUserPrompt_DoesNotIncludeResultRows()
    {
        var promptPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(promptPath, "Use only QueryResult.");
        var configuration = new ConfigurationBuilder().Build();
        var service = new ResultExplanationService(new HttpClient(), promptPath, configuration);

        var queryResult = new QueryResult
        {
            Operation = "filter",
            Success = true,
            Result = 1,
            Rows =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Rufnummer"] = "1001",
                    ["Vorname"] = "Max",
                    ["Nachname"] = "Müller",
                    ["Mail"] = "max.mueller@example.com"
                }
            ],
            Message = "Returned 1 matching rows.",
            Source = "CSharpQueryEngine"
        };

        var prompt = service.BuildUserPrompt("Zeige Max", queryResult);

        Assert.Contains("RowCount", prompt);
        Assert.DoesNotContain("max.mueller@example.com", prompt);
        Assert.DoesNotContain("Rufnummer", prompt);
    }
}
