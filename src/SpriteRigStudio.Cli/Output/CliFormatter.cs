namespace SpriteRigStudio.Cli.Output;

/// <summary>
/// Provides consistent console formatting for CLI output.
/// Supports colored output and column-aligned tables.
/// </summary>
public static class CliFormatter
{
    /// <summary>Writes an informational message in cyan.</summary>
    public static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>Writes a success message in green.</summary>
    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>Writes a warning message in yellow to stderr.</summary>
    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>Writes an error message in red to stderr.</summary>
    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>Writes a header/banner message in white.</summary>
    public static void WriteHeader(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a column-aligned table of items with a title header.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="title">Optional table title shown as a header.</param>
    /// <param name="items">The items to display.</param>
    /// <param name="columns">Column definitions (header text + value selector).</param>
    public static void WriteTable<T>(string? title, IEnumerable<T> items, params TableColumn<T>[] columns)
    {
        if (!string.IsNullOrEmpty(title))
        {
            WriteHeader($"=== {title} ===");
        }

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            WriteWarning("(no items)");
            return;
        }

        // Calculate column widths
        var widths = new int[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            widths[i] = columns[i].Header.Length;
        }

        foreach (var item in itemList)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                var value = columns[i].Selector(item) ?? string.Empty;
                if (value.Length > widths[i])
                    widths[i] = value.Length;
            }
        }

        // Clamp to terminal width (rough estimate)
        const int maxColumnWidth = 80;
        for (int i = 0; i < widths.Length; i++)
        {
            if (widths[i] > maxColumnWidth)
                widths[i] = maxColumnWidth;
        }

        const string separator = "  ";
        var format = string.Join(separator, widths.Select((w, i) => $"{{{i},-{w}}}"));
        var divider = string.Join(separator, widths.Select(w => new string('-', w)));

        // Header row with color
        var headerValues = columns.Select(c => c.Header).Cast<object>().ToArray();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(string.Format(format, headerValues));
        Console.WriteLine(divider);
        Console.ResetColor();

        // Data rows
        foreach (var item in itemList)
        {
            var values = columns.Select(c =>
            {
                var val = c.Selector(item) ?? string.Empty;
                return val.Length > maxColumnWidth ? val[..(maxColumnWidth - 3)] + "..." : val;
            }).Cast<object>().ToArray();

            Console.WriteLine(string.Format(format, values));
        }

        Console.WriteLine();
    }
}

/// <summary>
/// Defines a single column in a <see cref="CliFormatter"/> table.
/// </summary>
/// <typeparam name="T">The item type being displayed.</typeparam>
public class TableColumn<T>
{
    /// <summary>The column header text.</summary>
    public string Header { get; }

    /// <summary>Function that extracts the display value for this column from an item.</summary>
    public Func<T, string?> Selector { get; }

    public TableColumn(string header, Func<T, string?> selector)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }
}
