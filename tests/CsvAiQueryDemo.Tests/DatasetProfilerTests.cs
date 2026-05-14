using CsvAiQueryDemo.Services;

namespace CsvAiQueryDemo.Tests;

public sealed class DatasetProfilerTests
{
    [Fact]
    public void CreateProfile_CountsRowsAndColumns()
    {
        var rows = CreateRows();

        var profile = DatasetProfiler.CreateProfile("test", "test.csv", rows, ';');

        Assert.Equal(3, profile.RowCount);
        Assert.Equal(2, profile.ColumnCount);
        Assert.Contains(profile.Columns, column => column.Name == "Vorname");
    }

    [Fact]
    public void CreateProfile_CreatesTopValues()
    {
        var rows = CreateRows();

        var profile = DatasetProfiler.CreateProfile("test", "test.csv", rows, ';');
        var firstName = profile.Columns.Single(column => column.Name == "Vorname");

        Assert.Equal(2, firstName.TopValues["Max"]);
    }

    private static List<Dictionary<string, string>> CreateRows()
    {
        return
        [
            new(StringComparer.OrdinalIgnoreCase) { ["Vorname"] = "Max", ["Nachname"] = "Müller" },
            new(StringComparer.OrdinalIgnoreCase) { ["Vorname"] = "Anna", ["Nachname"] = "Schmidt" },
            new(StringComparer.OrdinalIgnoreCase) { ["Vorname"] = "Max", ["Nachname"] = "Meyer" }
        ];
    }
}
