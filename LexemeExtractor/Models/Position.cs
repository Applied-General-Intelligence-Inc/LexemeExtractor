namespace LexemeExtractor.Models;

/// <summary>
/// Represents a position in a source file with line and column information.
/// Supports both single positions and ranges (start/end positions).
/// </summary>
public record Position
{
    /// <summary>
    /// Line number (1-based)
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number (1-based)
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Length of the lexeme (optional, for single positions)
    /// </summary>
    public int? Length { get; init; }

    /// <summary>
    /// End line number (for ranges, optional)
    /// </summary>
    public int? EndLine { get; init; }

    /// <summary>
    /// End column number (for ranges, optional)
    /// </summary>
    public int? EndColumn { get; init; }

    /// <summary>
    /// Creates a single position with optional length
    /// </summary>
    public Position(int line, int column, int? length = null)
    {
        Line = line;
        Column = column;
        Length = length;
    }

    /// <summary>
    /// Creates a position range
    /// </summary>
    public Position(int startLine, int startColumn, int endLine, int endColumn)
    {
        Line = startLine;
        Column = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }

    /// <summary>
    /// Returns true if this position represents a range
    /// </summary>
    public bool IsRange => EndLine.HasValue || EndColumn.HasValue;

    /// <summary>
    /// Returns the effective end line (either EndLine or Line)
    /// </summary>
    public int EffectiveEndLine => EndLine ?? Line;

    /// <summary>
    /// Returns the effective end column (calculated from Column + Length or EndColumn)
    /// </summary>
    public int EffectiveEndColumn => EndColumn ?? (Length.HasValue ? Column + Length.Value - 1 : Column);

    /// <summary>
    /// Creates a new position with an incremented line number
    /// </summary>
    public Position IncrementLine(int increment = 1) => this with { Line = Line + increment };

    /// <summary>
    /// Creates a new position with an incremented column number
    /// </summary>
    public Position IncrementColumn(int increment = 1) => this with { Column = Column + increment };

    /// <summary>
    /// Creates a new position at the beginning of the next line
    /// </summary>
    public Position NextLine() => new(Line + 1, 1);

    /// <summary>
    /// Returns a human-readable string representation
    /// </summary>
    public override string ToString()
    {
        if (IsRange)
        {
            if (EndLine.HasValue && EndLine != Line)
            {
                return $"Line {Line}, Column {Column} - Line {EndLine}, Column {EffectiveEndColumn}";
            }
            else
            {
                return $"Line {Line}, Column {Column}-{EffectiveEndColumn}";
            }
        }
        else if (Length.HasValue && Length > 1)
        {
            return $"Line {Line}, Column {Column}-{EffectiveEndColumn}";
        }
        else
        {
            return $"Line {Line}, Column {Column}";
        }
    }
}
