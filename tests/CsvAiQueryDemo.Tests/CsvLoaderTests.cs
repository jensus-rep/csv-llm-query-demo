using CsvAiQueryDemo.Services;

namespace CsvAiQueryDemo.Tests;

public sealed class CsvLoaderTests
{
    [Fact]
    public void Load_ReadsCsvWithHeader()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        File.WriteAllText(filePath, "Rufnummer;Vorname;Nachname;Mail\n1;Max;Müller;max@example.com");

        try
        {
            var rows = new CsvLoader().Load(filePath, ';');

            Assert.Single(rows);
            Assert.Equal("Max", rows[0]["Vorname"]);
            Assert.Equal("Müller", rows[0]["Nachname"]);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
