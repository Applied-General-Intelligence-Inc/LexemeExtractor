using LexemeExtractor.Models;

namespace LexemeExtractor.Parsing;

/// <summary>
/// Position decoder implementing the full LexemeFileFormat.md specification
/// Handles all position encoding patterns including stateful position tracking
/// </summary>
public class PositionDecoder
{
    private Position _lastPosition = new(1, 1);
    private int _lastLine = 0;
    private int _lastColumn = 1;

    /// <summary>
    /// Decodes a position string into a Position object according to the grammar specification
    /// </summary>
    public Position DecodePosition(string positionStr) =>
        string.IsNullOrEmpty(positionStr)
            ? throw new ArgumentException("Position string cannot be null or empty", nameof(positionStr))
            : UpdateLastPosition(positionStr[0] switch
            {
                // Single character position encodings
                ':' => DecodeSingleCharPosition(1), // 1 character wide token at same line/column
                ';' => DecodeSingleCharPosition(2), // 2 character wide token at same line/column
                '@' => _lastPosition, // Same line/column as last
                '|' => DecodeSameLineIncrement(1), // Same line, column + 1
                '_' => DecodeSameLineIncrement(2), // Same line, column + 2

                // Complex position encodings
                '^' => DecodeCaretPattern(positionStr), // ^column - same line/column, ends in specified column
                '<' => DecodeLessThanPattern(positionStr), // <column - same line, specified column, 1 char wide
                '>' => DecodeGreaterThanPattern(positionStr), // >column - same line, specified column, 2 char wide
                '[' => DecodeBracketPattern(positionStr, false), // [column column - same line, specified range
                ']' => DecodeBracketPattern(positionStr, true), // ]column column - next line, specified range
                '=' => DecodeEqualsPattern(positionStr), // Various = patterns

                // Punctuation-based line increments (codes #21-#2F)
                '!' or '"' or '#' or '$' or '%' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or '-' or '.' or '/'
                    => DecodePunctuationPattern(positionStr),

                // Numeric absolute positions
                >= '0' and <= '9' => DecodeNumericPosition(positionStr),

                // Character-based column encoding (0x41-0x7E: A-Z, a-z, and punctuation)
                >= '\x41' and <= '\x7E' => DecodeLetterColumn(positionStr),

                _ => throw new FormatException($"Unknown position encoding: '{positionStr}'")
            });

    private Position UpdateLastPosition(Position position)
    {
        _lastPosition = position;
        _lastLine = position.Line;
        _lastColumn = position.Column + (position.Length ?? 0);
        return position;
    }

    /// <summary>
    /// Decode single character position encodings (:, ;)
    /// </summary>
    private Position DecodeSingleCharPosition(int width) =>
        new(_lastPosition.Line, _lastPosition.EffectiveEndColumn, width);

    /// <summary>
    /// Decode same line column increment (|, _)
    /// </summary>
    private Position DecodeSameLineIncrement(int increment) =>
        new(_lastPosition.Line, _lastPosition.EffectiveEndColumn + increment);

    /// <summary>
    /// Decode caret pattern: ^column
    /// </summary>
    private Position DecodeCaretPattern(string positionStr) =>
        positionStr.Length < 2
            ? throw new FormatException($"Invalid caret pattern: '{positionStr}'")
            : new(_lastPosition.Line, _lastPosition.EffectiveEndColumn, DecodeColumnValue(positionStr[1..]), DecodeColumnValue(positionStr[1..]));

    /// <summary>
    /// Decode less-than pattern: <column (1 character wide)
    /// </summary>
    private Position DecodeLessThanPattern(string positionStr) =>
        positionStr.Length < 2
            ? throw new FormatException($"Invalid less-than pattern: '{positionStr}'")
            : new(_lastPosition.Line, DecodeColumnValue(positionStr[1..]), 1);

    /// <summary>
    /// Decode greater-than pattern: >column (2 characters wide)
    /// </summary>
    private Position DecodeGreaterThanPattern(string positionStr) =>
        positionStr.Length < 2
            ? throw new FormatException($"Invalid greater-than pattern: '{positionStr}'")
            : new(_lastPosition.Line, DecodeColumnValue(positionStr[1..]), 2);

    /// <summary>
    /// Decode bracket patterns: [column column or ]column column
    /// </summary>
    private Position DecodeBracketPattern(string positionStr, bool nextLine)
    {
        if (positionStr.Length < 3)
            throw new FormatException($"Invalid bracket pattern: '{positionStr}'");

        var line = nextLine ? _lastLine + 1 : _lastLine;
        var lineChanged = nextLine || line != _lastPosition.Line;
        var parser = new PositionPatternParser(positionStr[1..], lineChanged, _lastPosition.EffectiveEndColumn);

        var startColumn = parser.ParseColumn();
        var endColumn = parser.ParseColumn();

        return new(line, startColumn, line, endColumn);
    }

    /// <summary>
    /// Decode equals patterns: =, ==, ===, =A, =A=B, etc.
    /// </summary>
    private Position DecodeEqualsPattern(string positionStr) =>
        positionStr.Length switch
        {
            1 => new(_lastLine, _lastColumn), // Just "="
            4 when positionStr.StartsWith("===") => new(_lastLine, _lastColumn, _lastLine, DecodeColumnValue(positionStr[3..])), // ===A
            3 when positionStr.StartsWith("==") => new(_lastLine, DecodeColumnValue(positionStr[2..])), // ==A
            _ when positionStr.StartsWith("===") => new(_lastLine, _lastColumn, 1), // ===
            _ when positionStr.StartsWith("==") => new(_lastLine, _lastColumn), // ==
            _ => DecodeComplexEqualsPattern(positionStr)
        };

    private Position DecodeComplexEqualsPattern(string positionStr)
    {
        var lineChanged = _lastLine != _lastPosition.Line;
        var parser = new PositionPatternParser(positionStr[1..], lineChanged, _lastColumn);
        var firstPart = parser.ParseColumnOrNumber();

        if (parser.HasMore() && parser.PeekChar() == '=')
        {
            parser.ConsumeChar('=');
            return new(_lastLine, firstPart, _lastLine, parser.ParseColumnOrNumber());
        }

        return new(_lastLine, firstPart);
    }

    /// <summary>
    /// Decode punctuation patterns for line increments
    /// </summary>
    private Position DecodePunctuationPattern(string positionStr)
    {
        var punctuationChar = positionStr[0];
        var increment = GetPunctuationIncrement(punctuationChar);
        var newLine = _lastLine + increment;

        if (positionStr.Length == 1)
        {
            // Simple punctuation pattern: just line increment
            return new(newLine, 1);
        }

        // Parse the remaining pattern after the punctuation character
        var parser = new PositionPatternParser(positionStr[1..], true, 1);
        var startColumn = parser.ParseColumn();

        if (!parser.HasMore())
        {
            // Single pattern: punctuation + column
            return new(newLine, startColumn);
        }

        // Complex pattern: punctuation + column + ending position
        // Parse the ending position information
        var endLine = newLine;
        var endColumn = startColumn;
        UpdateLastPosition(new(newLine, startColumn, endLine, endColumn));

        // Check what type of ending pattern we have
        var nextChar = parser.PeekChar();
        if (nextChar is >= (char)0x21 and <= (char)0x2F)  // Another punctuation increment
        {
            // Punctuation-based ending: punctuation + column
            var endPunctuationChar = nextChar;
            parser.ConsumeChar(endPunctuationChar);
            var endIncrement = GetPunctuationIncrement(endPunctuationChar);
            endLine = _lastLine + endIncrement;
            endColumn = parser.HasMore() ? parser.ParseColumn() : 1;
        }
        else if (nextChar == '=')
        {
            // Equals pattern ending: parse the remaining equals pattern
            var remainingPattern = positionStr[(positionStr.Length - parser.GetRemainingLength())..];
            var equalsPosition = DecodeEqualsPattern(remainingPattern);
            endLine = equalsPosition.EffectiveEndLine;
            endColumn = equalsPosition.EffectiveEndColumn;
        }
        else if (char.IsDigit(nextChar))
        {
            // Numeric ending: line and column
            endLine = parser.ParseNumber();
            endColumn = parser.ParseColumn();
        }
        else if (nextChar is >= '\x41' and <= '\x7E')
        {
            // Character-based column encoding for end column (same line)
            endColumn = parser.ParseColumn();
        }
        else
        {
            // Other pattern types - parse as column (same line)
            endColumn = parser.ParseColumn();
        }

        return new(newLine, startColumn, endLine, endColumn);
    }

    /// <summary>
    /// Decode numeric absolute positions
    /// </summary>
    private Position DecodeNumericPosition(string positionStr)
    {
        // For numeric positions, line numbers are absolute, so line has changed
        var parser = new PositionPatternParser(positionStr, true, _lastColumn);
        var startLine = parser.ParseNumber();
        var startColumn = parser.ParseColumn();

        return parser.HasMore()
            ? new(startLine, startColumn, parser.ParseNumber(), parser.ParseColumn())
            : new(startLine, startColumn);
    }

    /// <summary>
    /// Decode character-based column encoding (0x41-0x7E: A-Z, a-z, and punctuation)
    /// </summary>
    private Position DecodeLetterColumn(string positionStr)
    {
        var encodedValue = DecodeColumnValue(positionStr);

        // According to spec: if line number has just changed, this is new column number
        // If line number is same, this is increment to last column number
        var lineChanged = _lastLine != _lastPosition.Line;
        var column = lineChanged ? encodedValue : _lastColumn + encodedValue;

        return new(_lastLine, column);
    }

    /// <summary>
    /// Get line increment for punctuation characters (codes #21-#2F)
    /// </summary>
    private static int GetPunctuationIncrement(char punctuation) =>
        punctuation is >= '\x21' and <= '\x2F'  // ! through /
            ? punctuation - 0x20  // Subtract #20 to get increment (1-15)
            : throw new FormatException($"Invalid punctuation character for line increment: '{punctuation}'");

    /// <summary>
    /// Decode column value from string (character in range 0x41-0x7E or number)
    /// </summary>
    private static int DecodeColumnValue(string columnStr) =>
        string.IsNullOrEmpty(columnStr) ? 1 :
        columnStr == "=" ? 1 : // Same as last column (but this is handled elsewhere)
        columnStr[0] is >= '\x41' and <= '\x7E' ? columnStr[0] - 0x41 + 1 : // A-Z, a-z, and punctuation
        int.TryParse(columnStr, out var number) ? number :
        throw new FormatException($"Invalid column encoding: '{columnStr}'");
}
