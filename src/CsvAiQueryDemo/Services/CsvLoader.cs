using System.Text;

namespace CsvAiQueryDemo.Services;

public sealed class CsvLoader
{
    /// <summary>
    /// Loads a UTF-8 CSV file into case-insensitive row dictionaries keyed by header name.
    /// </summary>
    public IReadOnlyList<Dictionary<string, string>> Load(string filePath, char delimiter = ';')
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("CSV file was not found.", filePath);
        }

        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return [];
        }

        var headers = ParseLine(headerLine, delimiter);
        var rows = new List<Dictionary<string, string>>();

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseLine(line, delimiter);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                row[headers[i]] = i < values.Count ? values[i] : string.Empty;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<string> ParseLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                // RFC-style escaped quote inside a quoted value: "" becomes ".
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (character == delimiter && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString());
        return values;
    }
}
