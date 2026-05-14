using CsvAiQueryDemo.Models;
using CsvAiQueryDemo.Services;

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
        var originalFallback = Environment.GetEnvironmentVariable("OPENAI_ENABLE_FALLBACK");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        Environment.SetEnvironmentVariable("OPENAI_ENABLE_FALLBACK", "true");

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
            Environment.SetEnvironmentVariable("OPENAI_ENABLE_FALLBACK", originalFallback);
        }
    }

    [Fact]
    public void BuildRequestPayload_UsesAzureChatCompletionsShapeWhenAzureEndpointIsConfigured()
    {
        var originalProvider = Environment.GetEnvironmentVariable("OPENAI_PROVIDER");
        var originalAiProvider = Environment.GetEnvironmentVariable("AI_PROVIDER");
        var originalEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var originalDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
        Environment.SetEnvironmentVariable("OPENAI_PROVIDER", "azure");
        Environment.SetEnvironmentVariable("AI_PROVIDER", null);
        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://example-resource.cognitiveservices.azure.com/");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", "gpt-5.1");

        try
        {
            var payload = CreateService().BuildRequestPayload(
                "Wie viele Mail Adressen enthalten example.com?",
                CreateMinimalProfile());

            using var document = System.Text.Json.JsonDocument.Parse(payload);
            Assert.True(document.RootElement.TryGetProperty("messages", out var messages));
            Assert.Equal(2, messages.GetArrayLength());
            Assert.True(document.RootElement.TryGetProperty("max_completion_tokens", out _));
            Assert.True(document.RootElement.TryGetProperty("response_format", out var responseFormat));
            Assert.Equal("json_schema", responseFormat.GetProperty("type").GetString());
            Assert.False(document.RootElement.TryGetProperty("input", out _));
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_PROVIDER", originalProvider);
            Environment.SetEnvironmentVariable("AI_PROVIDER", originalAiProvider);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", originalEndpoint);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", originalDeployment);
        }
    }

    [Fact]
    public void BuildRequestPayload_AcceptsAiProviderAzureOpenAiAlias()
    {
        var originalProvider = Environment.GetEnvironmentVariable("OPENAI_PROVIDER");
        var originalAiProvider = Environment.GetEnvironmentVariable("AI_PROVIDER");
        var originalEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var originalDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
        var originalOpenAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        var originalAiModel = Environment.GetEnvironmentVariable("AI_MODEL");
        Environment.SetEnvironmentVariable("OPENAI_PROVIDER", null);
        Environment.SetEnvironmentVariable("AI_PROVIDER", "azure_openai");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://example-resource.cognitiveservices.azure.com/");
        Environment.SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", null);
        Environment.SetEnvironmentVariable("OPENAI_MODEL", null);
        Environment.SetEnvironmentVariable("AI_MODEL", "gpt-5.1-deployment");

        try
        {
            var payload = CreateService().BuildRequestPayload(
                "Wie viele Mail Adressen enthalten example.com?",
                CreateMinimalProfile());

            using var document = System.Text.Json.JsonDocument.Parse(payload);
            Assert.True(document.RootElement.TryGetProperty("messages", out _));
            Assert.True(document.RootElement.TryGetProperty("max_completion_tokens", out _));
            Assert.False(document.RootElement.TryGetProperty("input", out _));
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_PROVIDER", originalProvider);
            Environment.SetEnvironmentVariable("AI_PROVIDER", originalAiProvider);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", originalEndpoint);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", originalDeployment);
            Environment.SetEnvironmentVariable("OPENAI_MODEL", originalOpenAiModel);
            Environment.SetEnvironmentVariable("AI_MODEL", originalAiModel);
        }
    }

    private static QueryIntentService CreateService()
    {
        var promptPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(promptPath, "Return JSON only.");

        return new QueryIntentService(new HttpClient(), promptPath);
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
