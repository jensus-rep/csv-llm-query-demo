namespace CsvAiQueryDemo.Services;

public static class OpenAiHttpClientFactory
{
    public static HttpClient Create()
    {
        return new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false
        });
    }
}
