using CsvAiQueryDemo.Models;
using CsvAiQueryDemo.Services;
using Microsoft.Extensions.Configuration;

namespace CsvAiQueryDemo.Tests;

public sealed class QueryIntentServiceTests
{
    [Fact]
    public void BuildUserPrompt_DoesNotIncludeFullCsvRows()
    {
        var service = CreateService();
        var profile = new DatasetProfile
        {
            DatasetId = "test",
            FileName = "demodaten.csv",
            RowCount = 2,
            ColumnCount = 4,
            Delimiter = ";",
            DataRef = "local-memory:demodaten.csv",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Columns =
            [
                new ColumnProfile
                {
                    Name = "Vorname",
                    InferredType = "string",
                    EmptyCount = 0,
                    NonEmptyCount = 2,
                    UniqueCount = 2,
                    ExampleValues = ["Max", "Anna"],
                    TopValues = new Dictionary<string, int> { ["Max"] = 1, ["Anna"] = 1 }
                },
                new ColumnProfile
                {
                    Name = "Mail",
                    InferredType = "email",
                    EmptyCount = 0,
                    NonEmptyCount = 2,
                    UniqueCount = 2,
                    ExampleValues = ["max@example.com", "anna@example.com"],
                    TopValues = new Dictionary<string, int> { ["max@example.com"] = 1 }
                }
            ]
        };

        var prompt = service.BuildUserPrompt("Wie oft kommt der Vorname Max vor?", profile);

        Assert.Contains("DatasetProfile JSON", prompt);
        Assert.Contains("Allowed QueryIntent schema", prompt);
        Assert.DoesNotContain("1001;Max;Müller;max@example.com", prompt);
        Assert.DoesNotContain("\"Rufnummer\":\"1001\"", prompt);
        Assert.DoesNotContain("CSV rows", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateIntentAsync_UsesUnderstandableFallbackWhenApiKeyIsMissing()
    {
        var originalApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

        try
        {
            var result = await CreateService().CreateIntentAsync(
                "Wie viele Mail Adressen enthalten example.com?",
                CreateMinimalProfile());

            Assert.True(result.UsedFallback);
            Assert.Contains("not configured", result.Message);
            Assert.Equal("count", result.Intent.Operation);
            Assert.Equal("Mail", result.Intent.Column);
            Assert.Equal("contains", result.Intent.Operator);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalApiKey);
        }
    }

    private static QueryIntentService CreateService()
    {
        var promptPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(promptPath, "Return JSON only.");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OpenAI:Model"] = "test-model" })
            .Build();

        return new QueryIntentService(new HttpClient(), promptPath, configuration);
    }

    private static DatasetProfile CreateMinimalProfile()
    {
        return new DatasetProfile
        {
            DatasetId = "test",
            FileName = "demodaten.csv",
            RowCount = 2,
            ColumnCount = 1,
            Delimiter = ";",
            DataRef = "local-memory:demodaten.csv",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Columns =
            [
                new ColumnProfile
                {
                    Name = "Mail",
                    InferredType = "email",
                    EmptyCount = 0,
                    NonEmptyCount = 2,
                    UniqueCount = 2,
                    ExampleValues = ["max@example.com"],
                    TopValues = new Dictionary<string, int> { ["max@example.com"] = 1 }
                }
            ]
        };
    }
}
