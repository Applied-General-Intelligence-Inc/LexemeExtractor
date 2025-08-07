using LexemeExtractor.Models;
using StringContent = LexemeExtractor.Models.StringContent;

namespace LexemeExtractor.OutputFormatters;

/// <summary>
/// Streaming CSV formatter that outputs lexemes as they are parsed
/// </summary>
public class CsvStreamingFormatter : StreamingFormatterBase
{
    /// <summary>
    /// Creates a new CSV streaming formatter
    /// </summary>
    /// <param name="writer">TextWriter to output CSV to</param>
    public CsvStreamingFormatter(TextWriter writer) : base(writer)
    {
    }

    /// <summary>
    /// Writes the CSV header with column names
    /// </summary>
    public override void WriteHeader(FileHeader header)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(CsvStreamingFormatter));

        // Write CSV header row
        Writer.WriteLine("Type,NumberString,Number,Line,Column,EndLine,EndColumn,Length,IsRange,ContentType,Content");
        
        // Write file metadata as a comment (if supported by the consumer)
        Writer.WriteLine($"# Domain: {EscapeCsvValue(header.Domain)}");
        Writer.WriteLine($"# Filename: {EscapeCsvValue(header.Filename)}");
        Writer.WriteLine($"# Encoding: {EscapeCsvValue(header.Encoding)}");
    }

    /// <summary>
    /// Writes a single lexeme in CSV format
    /// </summary>
    protected override void WriteFormattedLexeme(Lexeme lexeme)
    {
        var type = EscapeCsvValue(lexeme.Type);
        var numberString = EscapeCsvValue(lexeme.NumberString);
        var number = lexeme.Number.ToString();
        var line = lexeme.Position.Line.ToString();
        var column = lexeme.Position.Column.ToString();
        var endLine = lexeme.Position.EndLine?.ToString() ?? "";
        var endColumn = lexeme.Position.EndColumn?.ToString() ?? "";
        var length = lexeme.Position.Length?.ToString() ?? "";
        var isRange = lexeme.Position.IsRange.ToString().ToLowerInvariant();
        var (contentType, contentValue) = GetContentInfo(lexeme.Content);

        Writer.WriteLine($"{type},{numberString},{number},{line},{column},{endLine},{endColumn},{length},{isRange},{contentType},{contentValue}");
    }

    /// <summary>
    /// Writes the CSV footer (just a comment with total count)
    /// </summary>
    public override void WriteFooter(int totalCount)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(CsvStreamingFormatter));

        Writer.WriteLine($"# Total lexemes: {totalCount}");
    }

    /// <summary>
    /// Escapes a value for CSV output
    /// </summary>
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If the value contains comma, quote, or newline, wrap in quotes and escape quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Gets content type and escaped value for CSV output
    /// </summary>
    private static (string Type, string Value) GetContentInfo(LexemeContent content)
    {
        return content switch
        {
            StringContent sc => ("string", EscapeCsvValue(sc.StringValue)),
            NumberContent nc => ("number", nc.NumberValue.ToString()),
            BooleanContent bc => ("boolean", bc.BooleanValue.ToString().ToLowerInvariant()),
            EmptyContent => ("empty", ""),
            _ => ("unknown", EscapeCsvValue(content.ToString()))
        };
    }
}
