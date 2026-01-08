using System.Text.Json;

namespace MigrationTool.Cli.Utilities;

/// <summary>
/// Utility class for consistent JSON output formatting.
/// </summary>
public static class JsonOutput
{
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions CompactOptions = new() { WriteIndented = false };

    /// <summary>
    /// Writes a success result to standard output as formatted JSON.
    /// </summary>
    /// <param name="result">The result object to serialize.</param>
    /// <param name="indented">Whether to format the output with indentation.</param>
    public static void WriteSuccess(object result, bool indented = true)
    {
        var options = indented ? IndentedOptions : CompactOptions;
        Console.WriteLine(JsonSerializer.Serialize(result, options));
    }

    /// <summary>
    /// Writes an error message to standard error as JSON and exits with code 1.
    /// </summary>
    /// <param name="message">The error message.</param>
    public static void WriteError(string message)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new { Error = message }));
        Environment.Exit(1);
    }

    /// <summary>
    /// Writes an exception to standard error as JSON and exits with code 1.
    /// </summary>
    /// <param name="ex">The exception to report.</param>
    public static void WriteException(Exception ex)
    {
        WriteError(ex.Message);
    }
}
