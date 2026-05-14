using CsvAiQueryDemo.Models;

namespace CsvAiQueryDemo.Services;

public static class DatasetProfiler
{
    /// <summary>
    /// Creates the small, non-row-level dataset summary that can safely be sent
    /// to the LLM for query interpretation.
    /// </summary>
    public static DatasetProfile CreateProfile(
        string datasetId,
        string fileName,
        IReadOnlyList<Dictionary<string, string>> rows,
        char delimiter)
    {
        var columnNames = rows.Count == 0 ? [] : rows[0].Keys.ToList();
        var columns = columnNames
            .Select(column => CreateColumnProfile(column, rows))
            .ToList();

        return new DatasetProfile
        {
            DatasetId = datasetId,
            FileName = fileName,
            RowCount = rows.Count,
            ColumnCount = columns.Count,
            Delimiter = delimiter.ToString(),
            Columns = columns,
            DataRef = "local-memory:demodaten.csv",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static ColumnProfile CreateColumnProfile(string column, IReadOnlyList<Dictionary<string, string>> rows)
    {
        var values = rows
            .Select(row => row.TryGetValue(column, out var value) ? value.Trim() : string.Empty)
            .ToList();

        var nonEmptyValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        var topValues = nonEmptyValues
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        // Examples and top values give the model enough vocabulary to map natural
        // language to columns without exposing the complete CSV.
        return new ColumnProfile
        {
            Name = column,
            InferredType = InferType(nonEmptyValues),
            EmptyCount = values.Count - nonEmptyValues.Count,
            NonEmptyCount = nonEmptyValues.Count,
            UniqueCount = nonEmptyValues.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            ExampleValues = nonEmptyValues.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList(),
            TopValues = topValues
        };
    }

    private static string InferType(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return "string";
        }

        if (values.All(value => int.TryParse(value, out _)))
        {
            return "integer";
        }

        if (values.All(value => value.Contains('@') && value.Contains('.')))
        {
            return "email";
        }

        return "string";
    }
}
