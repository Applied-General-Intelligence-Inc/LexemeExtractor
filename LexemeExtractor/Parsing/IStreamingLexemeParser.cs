using LexemeExtractor.Models;

namespace LexemeExtractor.Parsing;

/// <summary>
/// Interface for streaming lexeme parsers that process lexemes one at a time
/// </summary>
public interface IStreamingLexemeParser
{
    /// <summary>
    /// Parses a lexeme file from a TextReader, calling the callback for each lexeme
    /// </summary>
    /// <param name="reader">TextReader to parse from</param>
    /// <param name="onLexeme">Callback invoked for each parsed lexeme</param>
    /// <param name="sourceFilePath">Optional source file path for reference</param>
    /// <returns>File header and total count of lexemes processed</returns>
    (FileHeader Header, int Count) ParseStream(TextReader reader, Action<Lexeme> onLexeme, string? sourceFilePath = null);

    /// <summary>
    /// Parses a lexeme file from a file path, calling the callback for each lexeme
    /// </summary>
    /// <param name="filePath">Path to the lexeme file</param>
    /// <param name="onLexeme">Callback invoked for each parsed lexeme</param>
    /// <returns>File header and total count of lexemes processed</returns>
    (FileHeader Header, int Count) ParseFile(string filePath, Action<Lexeme> onLexeme);

    /// <summary>
    /// Parses a lexeme file from stdin, calling the callback for each lexeme
    /// </summary>
    /// <param name="onLexeme">Callback invoked for each parsed lexeme</param>
    /// <returns>File header and total count of lexemes processed</returns>
    (FileHeader Header, int Count) ParseStdin(Action<Lexeme> onLexeme);
}
