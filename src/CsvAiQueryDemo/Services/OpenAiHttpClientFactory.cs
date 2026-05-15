namespace CsvAiQueryDemo.Services;

public static class OpenAiHttpClientFactory
{
    public static HttpClient Create()
    {
        var proxySetting = Environment.GetEnvironmentVariable("OPENAI_USE_PROXY");
        var useProxy = !string.Equals(proxySetting, "false", StringComparison.OrdinalIgnoreCase);

        return new HttpClient(new SocketsHttpHandler
        {
            UseProxy = useProxy
        });
    }
}
