using Superpower;
using Superpower.Parsers;
using LexemeExtractor.Models;

namespace LexemeExtractor.Superpower;

/// <summary>
/// Superpower-based parser for lexeme name definition files
/// Parses lines in the format: name = :number TYPE;
/// where name can be optionally quoted, number is in base16, and TYPE is optional
/// </summary>
public static class NameDefinitionParser
{
    // Basic parsers
    private static readonly TextParser<string> QuotedNameParser =
        from _ in Character.EqualTo('\'')
        from name in Character.ExceptIn('\'').Many()
        from __ in Character.EqualTo('\'')
        select new string(name);

    private static readonly TextParser<string> UnquotedNameParser =
        Character.ExceptIn('=', ' ', '\t', '\n', '\r').AtLeastOnce()
            .Select(chars => new string(chars));

    private static readonly TextParser<string> NameParser =
        QuotedNameParser.Or(UnquotedNameParser);

    private static readonly TextParser<long> HexNumberParser =
        from _ in Character.EqualTo(':')
        from digits in Span.Regex(@"[0-9A-Fa-f]+")
        select Convert.ToInt64(digits.ToStringValue(), 16);

    private static readonly TextParser<string> DataTypeParser =
        Character.ExceptIn(';', '\n', '\r').AtLeastOnce()
            .Select(chars => new string(chars).Trim());

    // Main definition line parser
    private static readonly TextParser<LexemeNameDefinition> DefinitionLineParser =
        from name in NameParser
        from _ in Character.WhiteSpace.Many()
        from __ in Character.EqualTo('=')
        from ___ in Character.WhiteSpace.Many()
        from number in HexNumberParser
        from dataType in (from ____ in Character.WhiteSpace.AtLeastOnce()
                         from type in DataTypeParser
                         select type).OptionalOrDefault()
        from _____ in Character.EqualTo(';').OptionalOrDefault()
        from ______ in Character.WhiteSpace.Many()
        select new LexemeNameDefinition(name, number, dataType);
    
    /// <summary>
    /// Parses a lexeme name definition file and returns a dictionary by number (base36)
    /// </summary>
    /// <param name="filePath">Path to the definition file</param>
    /// <returns>Dictionary of lexeme name definitions keyed by number in base36</returns>
    public static Dictionary<string, LexemeNameDefinition> ParseFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            return new Dictionary<string, LexemeNameDefinition>();

        try
        {
            var result = new Dictionary<string, LexemeNameDefinition>();

            using var reader = new StreamReader(filePath);
            while (reader.ReadLine() is { } line)
            {
                var definition = ParseLine(line);
                if (definition == null) continue;
                result[definition.Number.ToBase36()] = definition;
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing definition file {filePath}", ex);
        }
    }

    /// <summary>
    /// Parses a single lexeme name definition line
    /// </summary>
    /// <param name="line">Line to parse</param>
    /// <returns>Parsed lexeme name definition or null if line doesn't match pattern</returns>
    private static LexemeNameDefinition? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#') || line.TrimStart().StartsWith("//"))
            return null;

        try
        {
            return DefinitionLineParser.Parse(line);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the expected definition file path for a given domain and lexeme file path.
    /// Searches in this order: same directory as input file, current directory,
    /// LEXEME_NAMES_FILES environment variable directory, executable directory.
    /// </summary>
    /// <param name="domain">Domain from the lexeme file header</param>
    /// <param name="lexemeFilePath">Path to the lexeme file</param>
    /// <returns>Path to the definition file if found, otherwise path in same directory as lexeme file</returns>
    public static string GetDefinitionFilePath(string domain, string lexemeFilePath)
    {
        var domainFileName = $"{domain}.txt";

        // Search order as specified:
        var searchDirectories = new List<string>();

        // 1. Same directory as input file
        var inputDirectory = Path.GetDirectoryName(lexemeFilePath);
        if (!string.IsNullOrEmpty(inputDirectory))
        {
            searchDirectories.Add(inputDirectory);
        }

        // 2. Program's current directory
        searchDirectories.Add(Directory.GetCurrentDirectory());

        // 3. Directory specified by LEXEME_NAMES_FILES environment variable
        var envDirectory = Environment.GetEnvironmentVariable("LEXEME_NAMES_FILES");
        if (!string.IsNullOrEmpty(envDirectory) && Directory.Exists(envDirectory))
        {
            searchDirectories.Add(envDirectory);
        }

        // 4. Executable's directory
        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(executablePath))
        {
            var executableDirectory = Path.GetDirectoryName(executablePath);
            if (!string.IsNullOrEmpty(executableDirectory))
            {
                searchDirectories.Add(executableDirectory);
            }
        }

        // Search in order and return first found file
        foreach (var directory in searchDirectories)
        {
            var candidatePath = Path.Combine(directory, domainFileName);
            if (System.IO.File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        // If not found anywhere, return path in same directory as lexeme file (for consistency)
        return Path.Combine(inputDirectory ?? "", domainFileName);
    }

    /// <summary>
    /// Converts a long value to its Base36 string representation
    /// </summary>
    /// <param name="value">Long value to convert</param>
    /// <returns>Base36 string representation</returns>
    private static string ToBase36(this long value)
    {
        if (value == 0)
            return "0";

        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        var result = new List<char>();
        var absValue = Math.Abs(value);

        while (absValue > 0)
        {
            result.Add(digits[(int)(absValue % 36)]);
            absValue /= 36;
        }

        if (value < 0)
            result.Add('-');

        result.Reverse();
        return new string(result.ToArray());
    }
}
