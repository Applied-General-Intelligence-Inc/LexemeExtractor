using System.Collections;

namespace LexemeExtractor.Models;

/// <summary>
/// Represents a complete parsed .lexemes file
/// </summary>
public record LexemeFile : IEnumerable<Lexeme>
{
    /// <summary>
    /// Header information from the file
    /// </summary>
    public FileHeader Header { get; init; } = new();

    /// <summary>
    /// Collection of lexemes in the file
    /// </summary>
    public IReadOnlyList<Lexeme> Lexemes { get; init; } = Array.Empty<Lexeme>();

    /// <summary>
    /// Source file path (for reference)
    /// </summary>
    public string? SourceFilePath { get; init; }

    /// <summary>
    /// Creates a new lexeme file
    /// </summary>
    public LexemeFile(FileHeader header, IEnumerable<Lexeme> lexemes, string? sourceFilePath = null)
    {
        Header = header;
        Lexemes = lexemes.ToList().AsReadOnly();
        SourceFilePath = sourceFilePath;
    }

    /// <summary>
    /// Default constructor for record initialization
    /// </summary>
    public LexemeFile() { }

    /// <summary>
    /// Total number of lexemes in the file
    /// </summary>
    public int Count => Lexemes.Count;

    /// <summary>
    /// Returns true if the file contains no lexemes
    /// </summary>
    public bool IsEmpty => Lexemes.Count == 0;

    /// <summary>
    /// Gets lexemes by index
    /// </summary>
    public Lexeme this[int index] => Lexemes[index];

    /// <summary>
    /// Filters lexemes by type
    /// </summary>
    public IEnumerable<Lexeme> GetLexemesByType(string type)
    {
        return Lexemes.Where(l => l.Type == type);
    }

    /// <summary>
    /// Gets all lexemes excluding trivia (whitespace and comments)
    /// </summary>
    public IEnumerable<Lexeme> GetNonTriviaLexemes()
    {
        return Lexemes.Where(l => !l.IsTrivia);
    }

    /// <summary>
    /// Gets lexemes within a specific line range
    /// </summary>
    public IEnumerable<Lexeme> GetLexemesInLineRange(int startLine, int endLine)
    {
        return Lexemes.Where(l => l.Position.Line >= startLine && l.Position.Line <= endLine);
    }

    /// <summary>
    /// Gets lexemes that have content
    /// </summary>
    public IEnumerable<Lexeme> GetLexemesWithContent()
    {
        return Lexemes.Where(l => l.HasContent);
    }

    /// <summary>
    /// Returns summary statistics about the file
    /// </summary>
    public LexemeFileStats GetStats()
    {
        var typeGroups = Lexemes.GroupBy(l => l.Type).ToList();
        var maxLine = Lexemes.Count > 0 ? Lexemes.Max(l => l.Position.EffectiveEndLine) : 0;
        
        return new LexemeFileStats
        {
            TotalLexemes = Count,
            UniqueTypes = typeGroups.Count,
            TypeCounts = typeGroups.ToDictionary(g => g.Key, g => g.Count()),
            LexemesWithContent = Lexemes.Count(l => l.HasContent),
            TriviaLexemes = Lexemes.Count(l => l.IsTrivia),
            MaxLineNumber = maxLine
        };
    }

    /// <summary>
    /// Implements IEnumerable<Lexeme>
    /// </summary>
    public IEnumerator<Lexeme> GetEnumerator() => Lexemes.GetEnumerator();

    /// <summary>
    /// Implements IEnumerable
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns a human-readable string representation
    /// </summary>
    public override string ToString()
    {
        return $"{Header} - {Count} lexemes";
    }
}

/// <summary>
/// Statistics about a lexeme file
/// </summary>
public record LexemeFileStats
{
    public int TotalLexemes { get; init; }
    public int UniqueTypes { get; init; }
    public Dictionary<string, int> TypeCounts { get; init; } = new();
    public int LexemesWithContent { get; init; }
    public int TriviaLexemes { get; init; }
    public int MaxLineNumber { get; init; }
}
