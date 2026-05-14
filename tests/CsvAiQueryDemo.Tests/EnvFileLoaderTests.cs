using CsvAiQueryDemo.Services;

namespace CsvAiQueryDemo.Tests;

public sealed class EnvFileLoaderTests
{
    [Fact]
    public void Load_ReadsEnvFileWithoutOverridingExistingValues()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.env");
        const string keyName = "CSVAIQUERYDEMO_TEST_API_KEY";
        const string modelName = "CSVAIQUERYDEMO_TEST_MODEL";
        var originalApiKey = Environment.GetEnvironmentVariable(keyName);
        var originalModel = Environment.GetEnvironmentVariable(modelName);
        Environment.SetEnvironmentVariable(keyName, "existing-key");
        Environment.SetEnvironmentVariable(modelName, null);
        File.WriteAllText(filePath, $"{keyName}=file-key\n{modelName}=\"gpt-5.4\"\n");

        try
        {
            EnvFileLoader.Load(filePath);

            Assert.Equal("existing-key", Environment.GetEnvironmentVariable(keyName));
            Assert.Equal("gpt-5.4", Environment.GetEnvironmentVariable(modelName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(keyName, originalApiKey);
            Environment.SetEnvironmentVariable(modelName, originalModel);
            File.Delete(filePath);
        }
    }
}
