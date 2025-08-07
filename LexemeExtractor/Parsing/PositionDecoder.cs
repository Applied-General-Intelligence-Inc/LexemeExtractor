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
        _lastColumn = position.Column + position.Length ?? 0;
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

        return positionStr.Length > 1
            ? new(newLine, DecodeColumnValue(positionStr[1..]))
            : new(newLine, 1);
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
    private static int GetPunctuationIncrement(char punctuation)
    {
        return punctuation switch
        {
            '!' => 1,  // #21 - #20 = 1
            '"' => 2,  // #22 - #20 = 2
            '#' => 3,  // #23 - #20 = 3
            '$' => 4,  // #24 - #20 = 4
            '%' => 5,  // #25 - #20 = 5
            '&' => 6,  // #26 - #20 = 6
            '\'' => 7, // #27 - #20 = 7
            '(' => 8,  // #28 - #20 = 8
            ')' => 9,  // #29 - #20 = 9
            '*' => 10, // #2A - #20 = 10
            '+' => 11, // #2B - #20 = 11
            ',' => 12, // #2C - #20 = 12
            '-' => 13, // #2D - #20 = 13
            '.' => 14, // #2E - #20 = 14
            '/' => 15, // #2F - #20 = 15
            _ => throw new FormatException($"Invalid punctuation character for line increment: '{punctuation}'")
        };
    }

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
