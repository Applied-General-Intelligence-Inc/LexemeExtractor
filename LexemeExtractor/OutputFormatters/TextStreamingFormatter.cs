using LexemeExtractor.Models;
using StringContent = LexemeExtractor.Models.StringContent;

namespace LexemeExtractor.OutputFormatters;

/// <summary>
/// Streaming text formatter that outputs lexemes in human-readable format
/// </summary>
public class TextStreamingFormatter : StreamingFormatterBase
{
    /// <summary>
    /// Creates a new text streaming formatter
    /// </summary>
    /// <param name="writer">TextWriter to output text to</param>
    public TextStreamingFormatter(TextWriter writer) : base(writer)
    {
    }

    /// <summary>
    /// Writes the text header with file information
    /// </summary>
    public override void WriteHeader(FileHeader header)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(TextStreamingFormatter));

        Writer.WriteLine("=== Lexeme File Analysis ===");
        Writer.WriteLine($"Domain: {header.Domain}");
        Writer.WriteLine($"Filename: {header.Filename}");
        Writer.WriteLine($"Encoding: {header.Encoding}");
        Writer.WriteLine();
        Writer.WriteLine("Lexemes:");
        Writer.WriteLine("--------");
    }

    /// <summary>
    /// Writes a single lexeme in human-readable text format
    /// </summary>
    protected override void WriteFormattedLexeme(Lexeme lexeme)
    {
        Writer.WriteLine($"#{lexeme.NumberString}({lexeme.Number}) [{lexeme.Type}] @ {lexeme.Position} = {FormatContent(lexeme.Content)}");
    }

    /// <summary>
    /// Writes the text footer with summary information
    /// </summary>
    public override void WriteFooter(int totalCount)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(TextStreamingFormatter));

        Writer.WriteLine();
        Writer.WriteLine("--------");
        Writer.WriteLine($"Total lexemes processed: {totalCount}");
    }

    /// <summary>
    /// Formats content for human-readable display
    /// </summary>
    private static string FormatContent(LexemeContent content)
    {
        return content switch
        {
            StringContent sc => $"\"{sc.StringValue}\"",
            NumberContent nc => nc.NumberValue.ToString(),
            BooleanContent bc => bc.BooleanValue ? "true" : "false",
            EmptyContent => "(empty)",
            _ => content.ToString()
        };
    }
}
