using CsvAiQueryDemo.Models;
using CsvAiQueryDemo.Services;

namespace CsvAiQueryDemo.Tests;

public sealed class QueryEngineTests
{
    [Fact]
    public void Execute_CountWorksWithoutLlm()
    {
        var result = CreateEngine().Execute(new QueryIntent
        {
            Operation = "count",
            Column = "Vorname",
            Operator = "equals",
            Value = "Max"
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.Result);
        Assert.Equal("CSharpQueryEngine", result.Source);
    }

    [Fact]
    public void Execute_DistinctWorks()
    {
        var result = CreateEngine().Execute(new QueryIntent
        {
            Operation = "distinct",
            Column = "Vorname"
        });

        var values = Assert.IsType<List<string>>(result.Result);
        Assert.Equal(3, values.Count);
    }

    [Fact]
    public void Execute_TopValuesWorks()
    {
        var result = CreateEngine().Execute(new QueryIntent
        {
            Operation = "top_values",
            Column = "Vorname"
        });

        var values = Assert.IsType<Dictionary<string, int>>(result.Result);
        Assert.Equal(2, values["Max"]);
    }

    [Fact]
    public void Execute_FilterWorks()
    {
        var result = CreateEngine().Execute(new QueryIntent
        {
            Operation = "filter",
            Column = "Nachname",
            Operator = "equals",
            Value = "Müller"
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public void Execute_GroupByCountWorks()
    {
        var result = CreateEngine().Execute(new QueryIntent
        {
            Operation = "group_by_count",
            GroupBy = "Nachname"
        });

        var values = Assert.IsType<Dictionary<string, int>>(result.Result);
        Assert.Equal(2, values["Müller"]);
    }

    [Fact]
    public void Execute_InvalidColumnReturnsUnderstandableError()
    {
        var result = CreateEngine().Execute(new QueryIntent
        {
            Operation = "count",
            Column = "Ort",
            Operator = "equals",
            Value = "Berlin"
        });

        Assert.False(result.Success);
        Assert.Contains("Unknown column", result.Message);
    }

    [Fact]
    public void Execute_InvalidOperationReturnsUnderstandableError()
    {
        var result = CreateEngine().Execute(new QueryIntent
        {
            Operation = "sum",
            Column = "Vorname"
        });

        Assert.False(result.Success);
        Assert.Contains("Unsupported operation", result.Message);
    }

    [Fact]
    public void SaveResult_WritesQueryResultJson()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var result = CreateEngine().Execute(new QueryIntent
        {
            Operation = "count",
            Column = "Vorname",
            Operator = "equals",
            Value = "Max"
        });

        try
        {
            QueryEngine.SaveResult(result, filePath);

            Assert.True(File.Exists(filePath));
            Assert.Contains("CSharpQueryEngine", File.ReadAllText(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static QueryEngine CreateEngine()
    {
        return new QueryEngine(
        [
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Vorname"] = "Max",
                ["Nachname"] = "Müller",
                ["Mail"] = "max.mueller@example.com"
            },
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Vorname"] = "Anna",
                ["Nachname"] = "Schmidt",
                ["Mail"] = "anna.schmidt@example.com"
            },
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Vorname"] = "Max",
                ["Nachname"] = "Meyer",
                ["Mail"] = "max.meyer@example.org"
            },
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Vorname"] = "Laura",
                ["Nachname"] = "Müller",
                ["Mail"] = "laura.mueller@example.com"
            }
        ]);
    }
}
