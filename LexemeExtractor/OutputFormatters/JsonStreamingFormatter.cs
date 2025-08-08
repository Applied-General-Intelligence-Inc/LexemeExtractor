using LexemeExtractor.Models;
using StringContent = LexemeExtractor.Models.StringContent;

namespace LexemeExtractor.OutputFormatters;

/// <summary>
/// Streaming JSON formatter that outputs lexemes as they are parsed
/// </summary>
public class JsonStreamingFormatter : StreamingFormatterBase
{
    private bool _firstLexeme = true;

    /// <summary>
    /// Creates a new JSON streaming formatter
    /// </summary>
    /// <param name="writer">TextWriter to output JSON to</param>
    public JsonStreamingFormatter(TextWriter writer) : base(writer)
    {
    }

    /// <summary>
    /// Writes the JSON header with file information
    /// </summary>
    public override void WriteHeader(FileHeader header)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(JsonStreamingFormatter));

        Writer.WriteLine("{");
        Writer.WriteLine($"  \"Domain\": \"{EscapeJsonString(header.Domain)}\",");
        Writer.WriteLine($"  \"Filename\": \"{EscapeJsonString(header.Filename)}\",");
        Writer.WriteLine($"  \"Encoding\": \"{EscapeJsonString(header.Encoding)}\",");
        Writer.WriteLine("  \"Lexemes\": [");
    }

    /// <summary>
    /// Writes a single lexeme in JSON format
    /// </summary>
    protected override void WriteFormattedLexeme(Lexeme lexeme)
    {
        if (!_firstLexeme)
        {
            Writer.WriteLine(",");
        }
        _firstLexeme = false;

        Writer.Write("    {");
        Writer.Write($"\"Type\": \"{EscapeJsonString(lexeme.Type)}\", ");
        Writer.Write($"\"NumberString\": \"{EscapeJsonString(lexeme.NumberString)}\", ");
        Writer.Write($"\"Number\": {lexeme.Number}, ");
        Writer.Write($"\"Name\": \"{EscapeJsonString(lexeme.NameDefinition?.Name ?? "")}\", ");
        Writer.Write($"\"ValueType\": \"{EscapeJsonString(lexeme.NameDefinition?.DataType ?? "None")}\", ");
        Writer.Write($"\"Position\": {SerializePositionJson(lexeme.Position)}, ");
        Writer.Write($"\"Content\": {SerializeContentJson(lexeme.Content)}");
        Writer.Write("}");
    }

    /// <summary>
    /// Writes the JSON footer with total count
    /// </summary>
    public override void WriteFooter(int totalCount)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(JsonStreamingFormatter));

        if (!_firstLexeme)
        {
            Writer.WriteLine();
        }
        Writer.WriteLine("  ],");
        Writer.WriteLine($"  \"LexemeCount\": {totalCount}");
        Writer.WriteLine("}");
    }

    /// <summary>
    /// Serializes position information as JSON string (AOT compatible)
    /// </summary>
    private static string SerializePositionJson(Position position)
    {
        var parts = new List<string>
        {
            $"\"Line\": {position.Line}",
            $"\"Column\": {position.Column}"
        };

        if (position.Length.HasValue)
            parts.Add($"\"Length\": {position.Length}");

        if (position.EndLine.HasValue)
            parts.Add($"\"EndLine\": {position.EndLine}");

        if (position.EndColumn.HasValue)
            parts.Add($"\"EndColumn\": {position.EndColumn}");

        parts.Add($"\"IsRange\": {(position.IsRange ? "true" : "false")}");

        return "{" + string.Join(", ", parts) + "}";
    }

    /// <summary>
    /// Serializes content information as JSON string (AOT compatible)
    /// </summary>
    private static string SerializeContentJson(LexemeContent content)
    {
        return content switch
        {
            StringContent sc => $"{{\"Type\": \"string\", \"Value\": \"{EscapeJsonString(sc.StringValue)}\"}}",
            NumberContent nc => $"{{\"Type\": \"number\", \"Value\": {nc.NumberValue}}}",
            BooleanContent bc => $"{{\"Type\": \"boolean\", \"Value\": {(bc.BooleanValue ? "true" : "false")}}}",
            EmptyContent => "{\"Type\": \"empty\", \"Value\": null}",
            _ => $"{{\"Type\": \"unknown\", \"Value\": \"{EscapeJsonString(content.ToString())}\"}}"
        };
    }

    /// <summary>
    /// Escapes a string for JSON output (AOT compatible)
    /// </summary>
    private static string EscapeJsonString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\b", "\\b")
            .Replace("\f", "\\f");
    }
}
