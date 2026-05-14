namespace CsvAiQueryDemo.Services;

public static class OpenAiHttpClientFactory
{
    public static HttpClient Create()
    {
        var useProxy = string.Equals(
            Environment.GetEnvironmentVariable("OPENAI_USE_PROXY"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        return new HttpClient(new SocketsHttpHandler
        {
            UseProxy = useProxy
        });
    }
}
