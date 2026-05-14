using System.Net.Http.Headers;

namespace CsvAiQueryDemo.Services;

internal enum OpenAiProvider
{
    OpenAiResponses,
    AzureChatCompletions
}

internal sealed class OpenAiProviderOptions
{
    private const string DefaultModel = "gpt-5.1";
    private const string DefaultResponsesEndpoint = "https://api.openai.com/v1/responses";
    private const string DefaultAzureApiVersion = "2024-12-01-preview";

    public required OpenAiProvider Provider { get; init; }
    public required string? ApiKey { get; init; }
    public required string ModelOrDeployment { get; init; }
    public required string RequestUri { get; init; }

    public static OpenAiProviderOptions FromEnvironment()
    {
        var providerName = FirstEnvironmentValue("OPENAI_PROVIDER", "AI_PROVIDER");
        var azureEndpoint = FirstEnvironmentValue("AZURE_OPENAI_ENDPOINT");
        var provider = IsAzureProvider(providerName)
            || !string.IsNullOrWhiteSpace(azureEndpoint)
                ? OpenAiProvider.AzureChatCompletions
                : OpenAiProvider.OpenAiResponses;

        if (provider == OpenAiProvider.AzureChatCompletions)
        {
            var deployment = FirstEnvironmentValue(
                    "AZURE_OPENAI_DEPLOYMENT",
                    "OPENAI_MODEL",
                    "AI_MODEL")
                ?? DefaultModel;
            var apiVersion = FirstEnvironmentValue(
                    "AZURE_OPENAI_API_VERSION",
                    "OPENAI_API_VERSION",
                    "AI_API_VERSION")
                ?? DefaultAzureApiVersion;

            return new OpenAiProviderOptions
            {
                Provider = OpenAiProvider.AzureChatCompletions,
                ApiKey = FirstEnvironmentValue(
                    "AZURE_OPENAI_API_KEY",
                    "OPENAI_API_KEY",
                    "AI_API_KEY"),
                ModelOrDeployment = deployment,
                RequestUri = string.IsNullOrWhiteSpace(azureEndpoint)
                    ? string.Empty
                    : BuildAzureChatCompletionsUri(azureEndpoint, deployment, apiVersion)
            };
        }

        return new OpenAiProviderOptions
        {
            Provider = OpenAiProvider.OpenAiResponses,
            ApiKey = FirstEnvironmentValue("OPENAI_API_KEY", "AI_API_KEY"),
            ModelOrDeployment = FirstEnvironmentValue("OPENAI_MODEL", "AI_MODEL") ?? DefaultModel,
            RequestUri = FirstEnvironmentValue("OPENAI_RESPONSES_ENDPOINT", "AI_RESPONSES_ENDPOINT")
                ?? DefaultResponsesEndpoint
        };
    }

    public void ApplyAuthentication(HttpRequestMessage request)
    {
        if (Provider == OpenAiProvider.AzureChatCompletions)
        {
            request.Headers.Add("api-key", ApiKey);
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
    }

    private static string BuildAzureChatCompletionsUri(string azureEndpoint, string deployment, string apiVersion)
    {
        var trimmedEndpoint = azureEndpoint.TrimEnd('/');
        return $"{trimmedEndpoint}/openai/deployments/{Uri.EscapeDataString(deployment)}/chat/completions?api-version={Uri.EscapeDataString(apiVersion)}";
    }

    private static bool IsAzureProvider(string? providerName)
    {
        return string.Equals(providerName, "azure", StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerName, "azure_openai", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstEnvironmentValue(params string[] names)
    {
        foreach (var name in names)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
