using LexemeExtractor.Models;

namespace LexemeExtractor.Parsing;

/// <summary>
/// Sequential parser for lexeme lines without delimiters
/// </summary>
public class LexemeLineParser(string line)
{
    private readonly string _line = line;
    private int _position = 0;

    /// <summary>
    /// Parse optional type character (A-O)
    /// </summary>
    public string ParseOptionalType()
    {
        if (_position >= _line.Length)
            return string.Empty;

        var c = _line[_position];
        return c is >= 'A' and <= 'O'
            ? (_position++, c.ToString()).Item2
            : string.Empty; // No type prefix
    }

    /// <summary>
    /// Parse radix36 number [0-9a-z]+ and return as string
    /// </summary>
    public string ParseRadix36Number()
    {
        if (_position >= _line.Length)
            throw new FormatException($"Expected radix36 number at position {_position} in line: {_line}");

        var start = _position;

        // Consume radix36 characters
        while (_position < _line.Length && IsRadix36Char(_line[_position]))
            _position++;

        return _position == start
            ? throw new FormatException($"Expected radix36 number at position {_position} in line: {_line}")
            : _line[start.._position];
    }



    /// <summary>
    /// Parse position encoding
    /// </summary>
    public string ParsePosition()
    {
        if (_position >= _line.Length)
            throw new FormatException($"Expected position encoding at position {_position} in line: {_line}");

        var start = _position;

        // Parse first pattern
        ParseSinglePositionPattern();

        // Check if we need to parse a second pattern (only for punctuation-based line increments)
        if (_position < _line.Length && IsPunctuationLineIncrement(_line[start]))
        {
            ParseSinglePositionPattern();
        }

        return _line[start.._position];
    }

    private void ParseSinglePositionPattern()
    {
        if (_position >= _line.Length)
            return;

        var currentChar = _line[_position];

        switch (currentChar)
        {
            case ':' or ';' or '@' or '|' or '_':
                // Single character position encodings
                _position++;
                break;

            case '^' or '<' or '>':
                // Single character followed by column
                _position++;
                ConsumeColumn();
                break;

            case '[' or ']':
                // Bracket followed by two columns
                _position++;
                ConsumeColumn();
                ConsumeColumn();
                break;

            case '=':
                // Equals-based encoding - can be complex
                ConsumeEqualsPattern();
                break;

            case '"' or '!' or '#' or '$' or '%' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or '-' or '.' or '/':
                // Punctuation-based line increment
                _position++;
                ConsumeColumn();
                break;

            default:
                // Could be a number (absolute position) or letter (column encoding)
                switch (currentChar)
                {
                    case var c when char.IsDigit(c):
                        ConsumeNumber(); // Line number
                        ConsumeColumn(); // Column
                        ConsumeNumber(); // End line number
                        ConsumeColumn(); // End column
                        break;
                    case var c when c is >= '\x41' and <= '\x7E':
                        ConsumeColumn(); // Character-based column encoding (0x41-0x7E)
                        break;
                    default:
                        throw new FormatException($"Unexpected position character '{currentChar}' at position {_position} in line: {_line}");
                }
                break;
        }
    }

    private static bool IsPunctuationLineIncrement(char c) =>
        c is '"' or '!' or '#' or '$' or '%' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or '-' or '.' or '/';

    /// <summary>
    /// Parse optional content (remainder of line)
    /// </summary>
    public LexemeContent ParseContent()
    {
        if (_position >= _line.Length)
            return LexemeContent.Empty;

        var contentStr = _line[_position..];
        _position = _line.Length; // Consume rest of line

        return ParseContentString(contentStr);
    }

    private static bool IsRadix36Char(char c) =>
        c is (>= '0' and <= '9') or (>= 'a' and <= 'z');

    private void ConsumeColumn()
    {
        if (_position >= _line.Length)
            return;

        var c = _line[_position];
        if (c == '=' || c is >= '\x41' and <= '\x7E') // A-Z, a-z, and punctuation
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
            _position++;
    }

    private void ConsumeEqualsPattern()
    {
        // Handle various = patterns: =, ==, ===, =A, =A=B, etc.
        _position++; // consume first =

        while (_position < _line.Length)
        {
            var c = _line[_position];
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
                return;
            }
            else
            {
                return;
            }
        }
    }

    private static long ParseRadix36(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return value.ToLowerInvariant()
            .Select(c => c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'z' => c - 'a' + 10,
                _ => throw new FormatException($"Invalid radix36 character: {c}")
            })
            .Aggregate(0L, (current, digit) => current * 36 + digit);
    }

    private static LexemeContent ParseContentString(string content) =>
        string.IsNullOrEmpty(content) ? LexemeContent.Empty : content switch
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
