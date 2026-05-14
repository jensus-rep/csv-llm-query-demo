using CsvAiQueryDemo.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CsvAiQueryDemo.Services;

public sealed class ResultExplanationService
{
    private const string DefaultModel = "gpt-5.2";
    private readonly HttpClient _httpClient;
    private readonly string _systemPrompt;
    private readonly string? _apiKey;
    private readonly string _model;

    public ResultExplanationService(HttpClient httpClient, string promptPath, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _systemPrompt = File.ReadAllText(promptPath);
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _model = Environment.GetEnvironmentVariable("OPENAI_MODEL")
            ?? configuration["OpenAI:Model"]
            ?? DefaultModel;
    }

    public async Task<string> ExplainAsync(string userQuestion, QueryResult queryResult, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return CreateLocalExplanation(queryResult);
        }

        var payload = new
        {
            model = _model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[] { new { type = "input_text", text = _systemPrompt } }
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = $"User question:\n{userQuestion}\n\nQueryResult JSON:\n{JsonSerializer.Serialize(queryResult, JsonOptions())}"
                        }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions()), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return CreateLocalExplanation(queryResult);
        }

        return ExtractOutputText(responseBody);
    }

    private static string CreateLocalExplanation(QueryResult queryResult)
    {
        if (!queryResult.Success)
        {
            return queryResult.Message;
        }

        return queryResult.Operation switch
        {
            "count" => $"Das Ergebnis ist {queryResult.Result}.",
            "filter" => $"Es wurden {queryResult.Rows.Count} passende Einträge gefunden.",
            "distinct" => "Die unterschiedlichen Werte wurden lokal ermittelt.",
            "top_values" => "Die häufigsten Werte wurden lokal ermittelt.",
            "group_by_count" => "Die Gruppenzählung wurde lokal ermittelt.",
            _ => queryResult.Message
        };
    }

    private static string ExtractOutputText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (document.RootElement.TryGetProperty("output_text", out var outputText))
        {
            return outputText.GetString() ?? string.Empty;
        }

        if (document.RootElement.TryGetProperty("output", out var outputItems))
        {
            foreach (var outputItem in outputItems.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentItems))
                {
                    continue;
                }

                foreach (var contentItem in contentItems.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }
            }
        }

        return string.Empty;
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions { WriteIndented = true };
    }
}
