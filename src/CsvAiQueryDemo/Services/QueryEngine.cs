using CsvAiQueryDemo.Models;
using System.Text.Json;

namespace CsvAiQueryDemo.Services;

public sealed class QueryEngine
{
    private static readonly HashSet<string> SupportedOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "count",
        "filter",
        "distinct",
        "top_values",
        "group_by_count"
    };

    private static readonly HashSet<string> SupportedOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "equals",
        "contains",
        "starts_with",
        "ends_with"
    };

    private readonly IReadOnlyList<Dictionary<string, string>> _rows;
    private readonly HashSet<string> _columns;

    public QueryEngine(IReadOnlyList<Dictionary<string, string>> rows)
    {
        _rows = rows;
        _columns = rows.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(rows[0].Keys, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Executes only the whitelisted operations from QueryIntent and returns a
    /// deterministic result. Unsupported intents are reported as failed results
    /// instead of being interpreted dynamically.
    /// </summary>
    public QueryResult Execute(QueryIntent intent)
    {
        if (!SupportedOperations.Contains(intent.Operation))
        {
            return Error(intent.Operation, $"Unsupported operation '{intent.Operation}'.");
        }

        return intent.Operation.ToLowerInvariant() switch
        {
            "count" => Count(intent),
            "filter" => Filter(intent),
            "distinct" => Distinct(intent),
            "top_values" => TopValues(intent),
            "group_by_count" => GroupByCount(intent),
            _ => Error(intent.Operation, $"Unsupported operation '{intent.Operation}'.")
        };
    }

    public static void SaveResult(QueryResult result, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(result, options));
    }

    private QueryResult Count(QueryIntent intent)
    {
        if (string.IsNullOrWhiteSpace(intent.Column))
        {
            return Success(intent.Operation, _rows.Count, [], $"Counted all rows: {_rows.Count}.");
        }

        var validation = ValidateColumnAndOperator(intent);
        if (validation is not null)
        {
            return validation;
        }

        var count = MatchingRows(intent).Count();
        return Success(intent.Operation, count, [], $"Counted {count} matching rows.");
    }

    private QueryResult Filter(QueryIntent intent)
    {
        var validation = ValidateColumnAndOperator(intent);
        if (validation is not null)
        {
            return validation;
        }

        var limit = Math.Clamp(intent.Limit ?? 50, 1, 500);
        var rows = MatchingRows(intent).Take(limit).ToList();
        return Success(intent.Operation, rows.Count, rows, $"Returned {rows.Count} matching rows.");
    }

    private QueryResult Distinct(QueryIntent intent)
    {
        var columnValidation = ValidateColumn(intent.Operation, intent.Column);
        if (columnValidation is not null)
        {
            return columnValidation;
        }

        var values = _rows
            .Select(row => row[intent.Column!])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Success(intent.Operation, values, [], $"Found {values.Count} distinct values.");
    }

    private QueryResult TopValues(QueryIntent intent)
    {
        var columnValidation = ValidateColumn(intent.Operation, intent.Column);
        if (columnValidation is not null)
        {
            return columnValidation;
        }

        var limit = Math.Clamp(intent.Limit ?? 5, 1, 50);
        var values = BuildCounts(intent.Column!, limit);
        return Success(intent.Operation, values, [], $"Returned top {values.Count} values.");
    }

    private QueryResult GroupByCount(QueryIntent intent)
    {
        var groupBy = string.IsNullOrWhiteSpace(intent.GroupBy) ? intent.Column : intent.GroupBy;
        var columnValidation = ValidateColumn(intent.Operation, groupBy);
        if (columnValidation is not null)
        {
            return columnValidation;
        }

        var limit = Math.Clamp(intent.Limit ?? 50, 1, 500);
        var values = BuildCounts(groupBy!, limit);
        return Success(intent.Operation, values, [], $"Grouped {values.Count} values.");
    }

    private Dictionary<string, int> BuildCounts(string column, int limit)
    {
        return _rows
            .Select(row => row[column])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private IEnumerable<Dictionary<string, string>> MatchingRows(QueryIntent intent)
    {
        return _rows.Where(row => Matches(row[intent.Column!], intent.Operator!, intent.Value ?? string.Empty));
    }

    private static bool Matches(string source, string queryOperator, string expected)
    {
        return queryOperator.ToLowerInvariant() switch
        {
            "equals" => string.Equals(source, expected, StringComparison.OrdinalIgnoreCase),
            "contains" => source.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "starts_with" => source.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
            "ends_with" => source.EndsWith(expected, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private QueryResult? ValidateColumnAndOperator(QueryIntent intent)
    {
        var columnValidation = ValidateColumn(intent.Operation, intent.Column);
        if (columnValidation is not null)
        {
            return columnValidation;
        }

        if (string.IsNullOrWhiteSpace(intent.Operator) || !SupportedOperators.Contains(intent.Operator))
        {
            return Error(intent.Operation, $"Unsupported or missing operator '{intent.Operator}'.");
        }

        return null;
    }

    private QueryResult? ValidateColumn(string operation, string? column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return Error(operation, "A column is required for this operation.");
        }

        if (!_columns.Contains(column))
        {
            return Error(operation, $"Unknown column '{column}'.");
        }

        return null;
    }

    private static QueryResult Success(string operation, object result, IReadOnlyList<Dictionary<string, string>> rows, string message)
    {
        return new QueryResult
        {
            Operation = operation,
            Success = true,
            Result = result,
            Rows = rows,
            Message = message,
            Source = "CSharpQueryEngine"
        };
    }

    private static QueryResult Error(string operation, string message)
    {
        return new QueryResult
        {
            Operation = operation,
            Success = false,
            Result = null,
            Rows = [],
            Message = message,
            Source = "CSharpQueryEngine"
        };
    }
}
