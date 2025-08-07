using LexemeExtractor.Models;

namespace LexemeExtractor.OutputFormatters;

/// <summary>
/// Base class for streaming lexeme formatters providing common functionality
/// </summary>
public abstract class StreamingFormatterBase : ILexemeFormatter
{
    protected readonly TextWriter Writer;
    protected bool Disposed;
    protected int LexemeCount;

    /// <summary>
    /// Creates a new streaming formatter
    /// </summary>
    /// <param name="writer">TextWriter to output formatted data to</param>
    protected StreamingFormatterBase(TextWriter writer)
    {
        Writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <summary>
    /// Called once at the beginning with file header information
    /// </summary>
    public abstract void WriteHeader(FileHeader header);

    /// <summary>
    /// Called for each lexeme as it's parsed
    /// </summary>
    public virtual void WriteLexeme(Lexeme lexeme)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(StreamingFormatterBase));

        LexemeCount++;
        WriteFormattedLexeme(lexeme);
    }

    /// <summary>
    /// Called once at the end to finalize the output
    /// </summary>
    public abstract void WriteFooter(int totalCount);

    /// <summary>
    /// Flushes any buffered output
    /// </summary>
    public virtual void Flush()
    {
        if (!Disposed)
            Writer.Flush();
    }

    /// <summary>
    /// Formats and writes a single lexeme - implemented by derived classes
    /// </summary>
    protected abstract void WriteFormattedLexeme(Lexeme lexeme);

    /// <summary>
    /// Disposes the formatter
    /// </summary>
    public virtual void Dispose()
    {
        if (!Disposed)
        {
            Flush();
            Disposed = true;
        }
    }
}
