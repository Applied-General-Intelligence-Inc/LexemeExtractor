using LexemeExtractor.Models;

namespace LexemeExtractor.OutputFormatters;

/// <summary>
/// Interface for streaming lexeme formatters that process lexemes one at a time
/// </summary>
public interface ILexemeFormatter : IDisposable
{
    /// <summary>
    /// Called once at the beginning with file header information
    /// </summary>
    /// <param name="header">File header containing domain, filename, and encoding</param>
    void WriteHeader(FileHeader header);

    /// <summary>
    /// Called for each lexeme as it's parsed
    /// </summary>
    /// <param name="lexeme">The lexeme to format and output</param>
    void WriteLexeme(Lexeme lexeme);

    /// <summary>
    /// Called once at the end to finalize the output
    /// </summary>
    /// <param name="totalCount">Total number of lexemes processed</param>
    void WriteFooter(int totalCount);

    /// <summary>
    /// Flushes any buffered output
    /// </summary>
    void Flush();
}
