using LexemeExtractor.Models;

namespace LexemeExtractor.Parsing;

/// <summary>
/// Manual parser for .lexemes files
/// This is a temporary implementation while we resolve ANTLR compatibility issues
/// </summary>
public class LexemeFileParser
{
    /// <summary>
    /// Parses a single lexeme line using sequential parsing (no delimiters)
    /// Format: [optional_type][radix36_number][position][content]
    /// </summary>
    public static Lexeme ParseLexemeLine(string line, PositionDecoder positionDecoder, Dictionary<string, LexemeNameDefinition>? nameDefinitions = null) =>
        string.IsNullOrEmpty(line)
            ? throw new FormatException("Empty lexeme line")
            : CreateLexeme(new LexemeLineParser(line), positionDecoder, nameDefinitions);

    private static Lexeme CreateLexeme(LexemeLineParser parser, PositionDecoder positionDecoder, Dictionary<string, LexemeNameDefinition>? nameDefinitions = null)
    {
        // Parse optional type (A-O)
        var type = parser.ParseOptionalType();

        // Parse radix36 number string
        var numberString = parser.ParseRadix36Number();

        // Parse position encoding
        var positionStr = parser.ParsePosition();
        var position = positionDecoder.DecodePosition(positionStr);

        // Parse optional content
        var content = parser.ParseContent();

        // Look up name definition by base36 string
        LexemeNameDefinition? nameDefinition = null;
        nameDefinitions?.TryGetValue(numberString, out nameDefinition);

        return new()
        {
            Type = type,
            NumberString = numberString,
            Position = position,
            Content = content,
            NameDefinition = nameDefinition
        };
    }

    /// <summary>
    /// Parses a Base36 string to a long value
    /// </summary>
    /// <param name="value">Base36 string to parse</param>
    /// <returns>Long value</returns>
    private static long ParseBase36(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return value.ToLowerInvariant()
            .Select(c => c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'z' => c - 'a' + 10,
                _ => throw new FormatException($"Invalid Base36 character: {c}")
            })
            .Aggregate(0L, (current, digit) => current * 36 + digit);
    }

}
