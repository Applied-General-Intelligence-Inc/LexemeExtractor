using LexemeExtractor.Models;
using System.Text.RegularExpressions;

namespace LexemeExtractor.Parsing;

/// <summary>
/// Manual parser for .lexemes files
/// This is a temporary implementation while we resolve ANTLR compatibility issues
/// </summary>
public class LexemeFileParser
{
    // Real lexeme format: [optional_type][radix36_number][position][content]
    // - optional_type: single character A-O (16 extra codes)
    // - radix36_number: [0-9a-z]+
    // - position: various encoded formats (see LexemeFileFormat.md)
    // - content: optional, starts with " for strings, +/- for numbers, ~t/~f for booleans
    // Everything concatenated without spaces - requires careful sequential parsing

    /// <summary>
    /// Parses a .lexemes file from a file path
    /// </summary>
    public static LexemeFile ParseFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        return ParseLines(lines, filePath);
    }

    /// <summary>
    /// Parses a .lexemes file from text content
    /// </summary>
    public static LexemeFile ParseText(string content, string? sourceFilePath = null)
    {
        var lines = content.Split('\n', StringSplitOptions.None)
                          .Select(line => line.TrimEnd('\r'))
                          .ToArray();
        return ParseLines(lines, sourceFilePath);
    }

    /// <summary>
    /// Parses a .lexemes file from an array of lines
    /// </summary>
    public static LexemeFile ParseLines(string[] lines, string? sourceFilePath = null)
    {
        if (lines.Length < 2)
        {
            throw new ArgumentException("File must have at least domain and filename lines", nameof(lines));
        }

        // Parse header - real format has 3 lines: domain, filename, encoding
        var domain = lines[0].Trim();
        var filename = lines[1].Trim();
        var encoding = lines.Length > 2 ? lines[2].Trim() : "UTF-8";
        var header = new FileHeader(domain, filename, encoding);

        // Parse lexemes
        var lexemes = new List<Lexeme>();
        var positionDecoder = new PositionDecoder();

        for (int i = 3; i < lines.Length; i++) // Start after header (domain, filename, encoding)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            try
            {
                var lexeme = ParseLexemeLine(line, positionDecoder);
                lexemes.Add(lexeme);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing line {i + 1}: {line}", ex);
            }
        }

        return new LexemeFile(header, lexemes, sourceFilePath);
    }

    /// <summary>
    /// Parses a single lexeme line using sequential parsing (no delimiters)
    /// Format: [optional_type][radix36_number][position][content]
    /// </summary>
    private static Lexeme ParseLexemeLine(string line, PositionDecoder positionDecoder)
    {
        if (string.IsNullOrEmpty(line))
            throw new FormatException("Empty lexeme line");

        var parser = new LexemeLineParser(line);

        // Parse optional type (A-O)
        var type = parser.ParseOptionalType();

        // Parse radix36 number
        var number = parser.ParseRadix36Number();

        // Parse position encoding
        var positionStr = parser.ParsePosition();
        var position = positionDecoder.DecodePosition(positionStr);

        // Parse optional content
        var content = parser.ParseContent();

        return new Lexeme
        {
            Type = type,
            Number = number,
            Position = position,
            Content = content
        };
    }


}

/// <summary>
/// Comprehensive position decoder implementing the full LexemeFileFormat.md specification
/// Handles all position encoding patterns including stateful position tracking
/// </summary>
public class PositionDecoder
{
    private Position _lastPosition = new(1, 1);
    private int _lastLine = 1;
    private int _lastColumn = 1;

    /// <summary>
    /// Decodes a position string into a Position object according to the grammar specification
    /// </summary>
    public Position DecodePosition(string positionStr)
    {
        if (string.IsNullOrEmpty(positionStr))
            throw new ArgumentException("Position string cannot be null or empty", nameof(positionStr));

        var position = positionStr[0] switch
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

            // Letter-based column encoding
            >= 'A' and <= 'Z' or >= 'a' and <= 'z' => DecodeLetterColumn(positionStr),

            _ => throw new FormatException($"Unknown position encoding: '{positionStr}'")
        };

        return UpdateLastPosition(position);
    }

    private Position UpdateLastPosition(Position position)
    {
        _lastPosition = position;
        _lastLine = position.Line;
        _lastColumn = position.Column;
        return position;
    }

    /// <summary>
    /// Decode single character position encodings (:, ;)
    /// </summary>
    private Position DecodeSingleCharPosition(int width)
    {
        return new Position(_lastLine, _lastColumn, width);
    }

    /// <summary>
    /// Decode same line column increment (|, _)
    /// </summary>
    private Position DecodeSameLineIncrement(int increment)
    {
        return new Position(_lastLine, _lastColumn + increment);
    }

    /// <summary>
    /// Decode caret pattern: ^column
    /// </summary>
    private Position DecodeCaretPattern(string positionStr)
    {
        if (positionStr.Length < 2)
            throw new FormatException($"Invalid caret pattern: '{positionStr}'");

        var endColumn = DecodeColumnValue(positionStr.Substring(1));
        return new Position(_lastLine, _lastColumn, endColumn, endColumn);
    }

    /// <summary>
    /// Decode less-than pattern: <column (1 character wide)
    /// </summary>
    private Position DecodeLessThanPattern(string positionStr)
    {
        if (positionStr.Length < 2)
            throw new FormatException($"Invalid less-than pattern: '{positionStr}'");

        var column = DecodeColumnValue(positionStr.Substring(1));
        return new Position(_lastLine, column, 1);
    }

    /// <summary>
    /// Decode greater-than pattern: >column (2 characters wide)
    /// </summary>
    private Position DecodeGreaterThanPattern(string positionStr)
    {
        if (positionStr.Length < 2)
            throw new FormatException($"Invalid greater-than pattern: '{positionStr}'");

        var column = DecodeColumnValue(positionStr.Substring(1));
        return new Position(_lastLine, column, 2);
    }

    /// <summary>
    /// Decode bracket patterns: [column column or ]column column
    /// </summary>
    private Position DecodeBracketPattern(string positionStr, bool nextLine)
    {
        if (positionStr.Length < 3)
            throw new FormatException($"Invalid bracket pattern: '{positionStr}'");

        var parser = new PositionPatternParser(positionStr.Substring(1));
        var startColumn = parser.ParseColumn();
        var endColumn = parser.ParseColumn();

        var line = nextLine ? _lastLine + 1 : _lastLine;
        return new Position(line, startColumn, line, endColumn);
    }

    /// <summary>
    /// Decode equals patterns: =, ==, ===, =A, =A=B, etc.
    /// </summary>
    private Position DecodeEqualsPattern(string positionStr)
    {
        if (positionStr.Length == 1) // Just "="
            return new Position(_lastLine, _lastColumn);

        if (positionStr.StartsWith("==="))
        {
            // ===A pattern - same line/column, single character width
            if (positionStr.Length == 4)
            {
                var endColumn = DecodeColumnValue(positionStr.Substring(3));
                return new Position(_lastLine, _lastColumn, _lastLine, endColumn);
            }
            return new Position(_lastLine, _lastColumn, 1);
        }

        if (positionStr.StartsWith("=="))
        {
            // ==A pattern - same line, column A
            if (positionStr.Length == 3)
            {
                var column = DecodeColumnValue(positionStr.Substring(2));
                return new Position(_lastLine, column);
            }
            return new Position(_lastLine, _lastColumn);
        }

        // =A=E pattern or =5 pattern
        var parser = new PositionPatternParser(positionStr.Substring(1));
        var firstPart = parser.ParseColumnOrNumber();

        if (parser.HasMore() && parser.PeekChar() == '=')
        {
            parser.ConsumeChar('=');
            var secondPart = parser.ParseColumnOrNumber();
            return new Position(_lastLine, firstPart, _lastLine, secondPart);
        }
        else
        {
            return new Position(_lastLine, firstPart);
        }
    }

    /// <summary>
    /// Decode punctuation patterns for line increments
    /// </summary>
    private Position DecodePunctuationPattern(string positionStr)
    {
        var punctuationChar = positionStr[0];
        var increment = GetPunctuationIncrement(punctuationChar);
        var newLine = _lastLine + increment;

        if (positionStr.Length > 1)
        {
            var column = DecodeColumnValue(positionStr.Substring(1));
            return new Position(newLine, column);
        }
        else
        {
            return new Position(newLine, 1);
        }
    }

    /// <summary>
    /// Decode numeric absolute positions
    /// </summary>
    private Position DecodeNumericPosition(string positionStr)
    {
        var parser = new PositionPatternParser(positionStr);
        var startLine = parser.ParseNumber();
        var startColumn = parser.ParseColumn();

        if (parser.HasMore())
        {
            var endLine = parser.ParseNumber();
            var endColumn = parser.ParseColumn();
            return new Position(startLine, startColumn, endLine, endColumn);
        }
        else
        {
            return new Position(startLine, startColumn);
        }
    }

    /// <summary>
    /// Decode letter-based column encoding
    /// </summary>
    private Position DecodeLetterColumn(string positionStr)
    {
        var column = DecodeColumnValue(positionStr);

        // According to spec: if line number has just changed, this is new column number
        // If line number is same, this is increment to last column number
        if (_lastLine == _lastPosition.Line)
        {
            // Same line - increment
            return new Position(_lastLine, _lastColumn + column);
        }
        else
        {
            // Line changed - absolute column
            return new Position(_lastLine, column);
        }
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
    /// Decode column value from string (letter or number)
    /// </summary>
    private static int DecodeColumnValue(string columnStr)
    {
        if (string.IsNullOrEmpty(columnStr))
            return 1;

        if (columnStr == "=")
            return 1; // Same as last column (but this is handled elsewhere)

        // Letter-based encoding: A=1, B=2, ..., Z=26, a=27, b=28, ..., z=52
        if (char.IsLetter(columnStr[0]))
        {
            char c = columnStr[0];
            if (c >= 'A' && c <= 'Z')
                return c - 'A' + 1;
            else if (c >= 'a' && c <= 'z')
                return c - 'a' + 27;
        }

        // Numeric encoding
        if (int.TryParse(columnStr, out var number))
            return number;

        throw new FormatException($"Invalid column encoding: '{columnStr}'");
    }
}

/// <summary>
/// Helper class for parsing complex position patterns
/// </summary>
internal class PositionPatternParser
{
    private readonly string _pattern;
    private int _position;

    public PositionPatternParser(string pattern)
    {
        _pattern = pattern;
        _position = 0;
    }

    public bool HasMore() => _position < _pattern.Length;

    public char PeekChar() => HasMore() ? _pattern[_position] : '\0';

    public void ConsumeChar(char expected)
    {
        if (!HasMore() || _pattern[_position] != expected)
            throw new FormatException($"Expected '{expected}' at position {_position} in pattern '{_pattern}'");
        _position++;
    }

    public int ParseNumber()
    {
        if (!HasMore() || !char.IsDigit(_pattern[_position]))
            throw new FormatException($"Expected number at position {_position} in pattern '{_pattern}'");

        int start = _position;
        while (HasMore() && char.IsDigit(_pattern[_position]))
            _position++;

        return int.Parse(_pattern.Substring(start, _position - start));
    }

    public int ParseColumn()
    {
        if (!HasMore())
            throw new FormatException($"Expected column at position {_position} in pattern '{_pattern}'");

        if (_pattern[_position] == '=')
        {
            _position++;
            return 1; // Same as last column
        }

        if (char.IsLetter(_pattern[_position]))
        {
            char c = _pattern[_position++];
            if (c >= 'A' && c <= 'Z')
                return c - 'A' + 1;
            else if (c >= 'a' && c <= 'z')
                return c - 'a' + 27;
        }

        if (char.IsDigit(_pattern[_position]))
        {
            return ParseNumber();
        }

        throw new FormatException($"Invalid column character at position {_position} in pattern '{_pattern}'");
    }

    public int ParseColumnOrNumber()
    {
        if (char.IsDigit(PeekChar()))
            return ParseNumber();
        else
            return ParseColumn();
    }
}

/// <summary>
/// Sequential parser for lexeme lines without delimiters
/// </summary>
public class LexemeLineParser
{
    private readonly string _line;
    private int _position;

    public LexemeLineParser(string line)
    {
        _line = line;
        _position = 0;
    }

    /// <summary>
    /// Parse optional type character (A-O)
    /// </summary>
    public string ParseOptionalType()
    {
        if (_position >= _line.Length)
            return string.Empty;

        char c = _line[_position];
        if (c >= 'A' && c <= 'O')
        {
            _position++;
            return c.ToString();
        }

        return string.Empty; // No type prefix
    }

    /// <summary>
    /// Parse radix36 number [0-9a-z]+
    /// </summary>
    public long ParseRadix36Number()
    {
        if (_position >= _line.Length)
            throw new FormatException($"Expected radix36 number at position {_position} in line: {_line}");

        int start = _position;

        // Consume radix36 characters
        while (_position < _line.Length && IsRadix36Char(_line[_position]))
        {
            _position++;
        }

        if (_position == start)
            throw new FormatException($"Expected radix36 number at position {_position} in line: {_line}");

        string numberStr = _line.Substring(start, _position - start);
        return ParseRadix36(numberStr);
    }

    /// <summary>
    /// Parse position encoding - this is the tricky part
    /// </summary>
    public string ParsePosition()
    {
        if (_position >= _line.Length)
            throw new FormatException($"Expected position encoding at position {_position} in line: {_line}");

        int start = _position;

        // Position parsing depends on the first character
        char firstChar = _line[_position];

        switch (firstChar)
        {
            case ':':
            case ';':
            case '@':
            case '|':
            case '_':
                // Single character position encodings
                _position++;
                break;

            case '^':
            case '<':
            case '>':
                // Single character followed by column
                _position++;
                ConsumeColumn();
                break;

            case '[':
            case ']':
                // Bracket followed by two columns
                _position++;
                ConsumeColumn();
                ConsumeColumn();
                break;

            case '=':
                // Equals-based encoding - can be complex
                ConsumeEqualsPattern();
                break;

            case '"':
            case '!':
            case '#':
            case '$':
            case '%':
            case '&':
            case '\'':
            case '(':
            case ')':
            case '*':
            case '+':
            case ',':
            case '-':
            case '.':
            case '/':
                // Punctuation-based line increment
                _position++;
                ConsumeColumn();
                break;

            default:
                // Could be a number (absolute position) or letter (column encoding)
                if (char.IsDigit(firstChar))
                {
                    ConsumeNumber(); // Line number
                    ConsumeColumn(); // Column
                    ConsumeNumber(); // End line number
                    ConsumeColumn(); // End column
                }
                else if (char.IsLetter(firstChar))
                {
                    ConsumeColumn(); // Letter-based column encoding
                }
                else
                {
                    throw new FormatException($"Unexpected position character '{firstChar}' at position {_position} in line: {_line}");
                }
                break;
        }

        return _line.Substring(start, _position - start);
    }

    /// <summary>
    /// Parse optional content (remainder of line)
    /// </summary>
    public LexemeContent ParseContent()
    {
        if (_position >= _line.Length)
            return LexemeContent.Empty;

        string contentStr = _line.Substring(_position);
        _position = _line.Length; // Consume rest of line

        return ParseContentString(contentStr);
    }

    private static bool IsRadix36Char(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z');
    }

    private void ConsumeColumn()
    {
        if (_position >= _line.Length)
            return;

        char c = _line[_position];
        if (c == '=')
        {
            _position++;
        }
        else if (char.IsLetter(c))
        {
            _position++;
        }
        else if (char.IsDigit(c))
        {
            ConsumeNumber();
        }
    }

    private void ConsumeNumber()
    {
        while (_position < _line.Length && char.IsDigit(_line[_position]))
        {
            _position++;
        }
    }

    private void ConsumeEqualsPattern()
    {
        // Handle various = patterns: =, ==, ===, =A, =A=B, etc.
        _position++; // consume first =

        while (_position < _line.Length)
        {
            char c = _line[_position];
            if (c == '=')
            {
                _position++;
            }
            else if (char.IsLetterOrDigit(c))
            {
                _position++;
                // Check for another = after the letter/digit
                if (_position < _line.Length && _line[_position] == '=')
                {
                    _position++;
                    // Consume what follows the second =
                    ConsumeColumn();
                }
                break;
            }
            else
            {
                break;
            }
        }
    }

    private static long ParseRadix36(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        long result = 0;
        foreach (char c in value.ToLowerInvariant())
        {
            int digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'z' => c - 'a' + 10,
                _ => throw new FormatException($"Invalid radix36 character: {c}")
            };
            result = result * 36 + digit;
        }
        return result;
    }

    private static LexemeContent ParseContentString(string content)
    {
        if (string.IsNullOrEmpty(content))
            return LexemeContent.Empty;

        return content switch
        {
            ['\"', .. var stringContent] => new Models.StringContent(stringContent),
            ['+', .. var digits] => new Models.NumberContent(ParseRadix36(digits)),
            ['-', .. var digits] => new Models.NumberContent(-ParseRadix36(digits)),
            "~t" => new Models.BooleanContent(true),
            "~f" => new Models.BooleanContent(false),
            _ when char.IsDigit(content[0]) => new Models.NumberContent(ParseRadix36(content)),
            _ => new Models.StringContent(content) // Default to string for unknown content
        };
    }
}
