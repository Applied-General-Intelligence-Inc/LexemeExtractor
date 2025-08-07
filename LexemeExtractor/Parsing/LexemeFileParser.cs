using LexemeExtractor.Models;

namespace LexemeExtractor.Parsing;

/// <summary>
/// Manual parser for .lexemes files
/// This is a temporary implementation while we resolve ANTLR compatibility issues
/// </summary>
public class LexemeFileParser : IStreamingLexemeParser
{
    // Lexeme format: [optional_type][radix36_number][position][content]
    // - optional_type: single character A-O (16 extra codes)
    // - radix36_number: [0-9a-z]+
    // - position: various encoded formats (see LexemeFileFormat.md)
    // - content: optional, starts with " for strings, +/- for numbers, ~t/~f for booleans
    // Everything concatenated without spaces - requires careful sequential parsing

    /// <summary>
    /// Parses a .lexemes file from a file path using streaming
    /// </summary>
    public static LexemeFile ParseFile(string filePath)
    {
        using var reader = new StreamReader(filePath);
        return ParseStream(reader, filePath);
    }

    /// <summary>
    /// Parses a .lexemes file from stdin using streaming
    /// </summary>
    public static LexemeFile ParseStdin() =>
        ParseStream(Console.In, "<stdin>");

    /// <summary>
    /// Parses a .lexemes file from text content
    /// </summary>
    public static LexemeFile ParseText(string content, string? sourceFilePath = null)
    {
        using var reader = new StringReader(content);
        return ParseStream(reader, sourceFilePath);
    }

    // IStreamingLexemeParser implementation

    /// <summary>
    /// Parses a lexeme file from a TextReader, calling the callback for each lexeme
    /// </summary>
    public (FileHeader Header, int Count) ParseStream(TextReader reader, Action<Lexeme> onLexeme, string? sourceFilePath = null)
    {
        // Parse header - real format has 3 lines: domain, filename, encoding
        var domain = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing domain line");
        var filename = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing filename line");
        var encoding = reader.ReadLine()?.Trim() ?? "UTF-8";
        var header = new FileHeader(domain, filename, encoding);

        // Stream lexemes
        var positionDecoder = new PositionDecoder();
        var lineNumber = 4; // Start counting after header
        var count = 0;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line))
            {
                lineNumber++;
                continue;
            }

            try
            {
                var lexeme = ParseLexemeLine(line, positionDecoder);
                onLexeme(lexeme);
                count++;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing line {lineNumber}: {line}", ex);
            }

            lineNumber++;
        }

        return (header, count);
    }

    /// <summary>
    /// Parses a lexeme file from a file path, calling the callback for each lexeme
    /// </summary>
    public (FileHeader Header, int Count) ParseFile(string filePath, Action<Lexeme> onLexeme)
    {
        using var reader = new StreamReader(filePath);
        return ParseStream(reader, onLexeme, filePath);
    }

    /// <summary>
    /// Parses a lexeme file from stdin, calling the callback for each lexeme
    /// </summary>
    public (FileHeader Header, int Count) ParseStdin(Action<Lexeme> onLexeme) =>
        ParseStream(Console.In, onLexeme, "<stdin>");

    /// <summary>
    /// Parses a .lexemes file from an array of lines (legacy method for compatibility)
    /// </summary>
    public static LexemeFile ParseLines(string[] lines, string? sourceFilePath = null) =>
        ParseText(string.Join('\n', lines), sourceFilePath);

    /// <summary>
    /// Parses a .lexemes file from a TextReader using streaming
    /// </summary>
    public static LexemeFile ParseStream(TextReader reader, string? sourceFilePath = null)
    {
        // Parse header - real format has 3 lines: domain, filename, encoding
        var domain = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing domain line");
        var filename = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing filename line");
        var encoding = reader.ReadLine()?.Trim() ?? "UTF-8";
        var header = new FileHeader(domain, filename, encoding);

        // Load lexeme name definitions if available
        Dictionary<long, LexemeNameDefinition>? nameDefinitions = null;
        if (!string.IsNullOrEmpty(sourceFilePath))
        {
            var definitionFilePath = LexemeNameDefinitionParser.GetDefinitionFilePath(domain, sourceFilePath);
            var definitions = LexemeNameDefinitionParser.ParseFile(definitionFilePath);
            nameDefinitions = LexemeNameDefinitionParser.CreateNumberLookup(definitions);
        }

        // Stream lexemes
        var lexemes = new List<Lexeme>();
        var positionDecoder = new PositionDecoder();
        var lineNumber = 4; // Start counting after header

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line))
            {
                lineNumber++;
                continue;
            }

            try
            {
                var lexeme = ParseLexemeLine(line, positionDecoder, nameDefinitions);
                lexemes.Add(lexeme);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing line {lineNumber}: {line}", ex);
            }

            lineNumber++;
        }

        return new(header, lexemes, sourceFilePath);
    }

    /// <summary>
    /// Parses a single lexeme line using sequential parsing (no delimiters)
    /// Format: [optional_type][radix36_number][position][content]
    /// </summary>
    public static Lexeme ParseLexemeLine(string line, PositionDecoder positionDecoder, Dictionary<long, LexemeNameDefinition>? nameDefinitions = null) =>
        string.IsNullOrEmpty(line)
            ? throw new FormatException("Empty lexeme line")
            : CreateLexeme(new LexemeLineParser(line), positionDecoder, nameDefinitions);

    private static Lexeme CreateLexeme(LexemeLineParser parser, PositionDecoder positionDecoder, Dictionary<long, LexemeNameDefinition>? nameDefinitions = null)
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

        // Look up name definition by number
        var number = ParseBase36(numberString);
        LexemeNameDefinition? nameDefinition = null;
        nameDefinitions?.TryGetValue(number, out nameDefinition);

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
