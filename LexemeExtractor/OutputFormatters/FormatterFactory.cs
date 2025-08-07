namespace LexemeExtractor.OutputFormatters;

/// <summary>
/// Factory for creating lexeme formatters based on format type
/// </summary>
public static class FormatterFactory
{
    /// <summary>
    /// Creates a formatter for the specified format
    /// </summary>
    /// <param name="format">Format type (json, csv, text, xml)</param>
    /// <param name="writer">TextWriter to output to</param>
    /// <returns>Configured formatter instance</returns>
    public static ILexemeFormatter CreateFormatter(string format, TextWriter writer)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonStreamingFormatter(writer),
            "csv" => new CsvStreamingFormatter(writer),
            "xml" => new XmlStreamingFormatter(writer),
            "text" or _ => new TextStreamingFormatter(writer)
        };
    }

    /// <summary>
    /// Gets the file extension for a given format
    /// </summary>
    /// <param name="format">Format type</param>
    /// <returns>File extension including the dot</returns>
    public static string GetFileExtension(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => ".json",
            "csv" => ".csv",
            "xml" => ".xml",
            _ => ".txt"
        };
    }

    /// <summary>
    /// Gets all supported format names
    /// </summary>
    /// <returns>Array of supported format names</returns>
    public static string[] GetSupportedFormats()
    {
        return ["text", "json", "csv", "xml"];
    }
}
