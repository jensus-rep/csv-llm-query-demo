using CsvAiQueryDemo.Models;
using System.Text;
using System.Text.Json;

namespace CsvAiQueryDemo.Services;

public sealed class ResultExplanationService
{
    private readonly HttpClient _httpClient;
    private readonly string _systemPrompt;
    private readonly OpenAiProviderOptions _providerOptions;

    public ResultExplanationService(HttpClient httpClient, string promptPath)
    {
        _httpClient = httpClient;
        _systemPrompt = File.ReadAllText(promptPath);
        _providerOptions = OpenAiProviderOptions.FromEnvironment();
    }

    public async Task<ResultExplanationGeneration> ExplainAsync(string userQuestion, QueryResult queryResult, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_providerOptions.ApiKey))
        {
            return CreateLocalGeneration(queryResult);
        }

        if (_providerOptions.Provider == OpenAiProvider.AzureChatCompletions
            && string.IsNullOrWhiteSpace(_providerOptions.RequestUri))
        {
            return CreateLocalGeneration(queryResult);
        }

        var payload = BuildRequestPayload(userQuestion, queryResult);
        using var request = new HttpRequestMessage(HttpMethod.Post, _providerOptions.RequestUri);
        _providerOptions.ApplyAuthentication(request);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CreateLocalGeneration(queryResult);
            }

            return new ResultExplanationGeneration(ExtractOutputText(responseBody), ExtractUsage(responseBody));
        }
        catch (HttpRequestException)
        {
            return CreateLocalGeneration(queryResult);
        }
        catch (TaskCanceledException)
        {
            return CreateLocalGeneration(queryResult);
        }
    }

    public string BuildUserPrompt(string userQuestion, QueryResult queryResult)
    {
        var safeResult = new
        {
            queryResult.Operation,
            queryResult.Success,
            queryResult.Result,
            RowCount = queryResult.Rows.Count,
            queryResult.Message,
            queryResult.Source
        };

        return $"User question:\n{userQuestion}\n\nQueryResult JSON:\n{JsonSerializer.Serialize(safeResult, JsonOptions())}";
    }

    public string BuildRequestPayload(string userQuestion, QueryResult queryResult)
    {
        var userPrompt = BuildUserPrompt(userQuestion, queryResult);
        if (_providerOptions.Provider == OpenAiProvider.AzureChatCompletions)
        {
            var azurePayload = new
            {
                messages = new object[]
                {
                    new { role = "system", content = _systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_completion_tokens = 4096
            };

            return JsonSerializer.Serialize(azurePayload, JsonOptions());
        }

        var payload = new
        {
            model = _providerOptions.ModelOrDeployment,
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
                            text = userPrompt
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload, JsonOptions());
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

    private static ResultExplanationGeneration CreateLocalGeneration(QueryResult queryResult)
    {
        return new ResultExplanationGeneration(CreateLocalExplanation(queryResult), TokenUsage.Zero);
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

        if (document.RootElement.TryGetProperty("choices", out var choices))
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.TryGetProperty("message", out var message)
                    && message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private static TokenUsage ExtractUsage(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("usage", out var usage))
        {
            return TokenUsage.Zero;
        }

        var inputTokens = GetIntProperty(usage, "input_tokens")
            ?? GetIntProperty(usage, "prompt_tokens")
            ?? 0;
        var outputTokens = GetIntProperty(usage, "output_tokens")
            ?? GetIntProperty(usage, "completion_tokens")
            ?? 0;
        var totalTokens = GetIntProperty(usage, "total_tokens") ?? inputTokens + outputTokens;

        return new TokenUsage(inputTokens, outputTokens, totalTokens);
    }

    private static int? GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result))
        {
            return result;
        }

        return null;
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions { WriteIndented = true };
    }
}
