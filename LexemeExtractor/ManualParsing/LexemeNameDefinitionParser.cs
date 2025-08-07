using System.Text.RegularExpressions;
using LexemeExtractor.Models;

namespace LexemeExtractor.ManualParsing;

/// <summary>
/// Parser for lexeme name definition files
/// Parses lines in the format: name = :number TYPE;
/// where name can be optionally quoted, number is in base16, and TYPE is optional
/// </summary>
public static class LexemeNameDefinitionParser
{
    // Regex pattern to match: optional_quotes_name = :hex_number optional_type;
    // Groups: 1=name (with or without quotes), 2=hex_number, 3=type (optional)
    private static readonly Regex DefinitionPattern = new(
        @"^(?:'([^']+)'|([^=\s]+))\s*=\s*:([0-9A-Fa-f]+)(?:\s+([^;]+))?\s*;?\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a lexeme name definition file and returns a dictionary by number (base36)
    /// </summary>
    /// <param name="filePath">Path to the definition file</param>
    /// <returns>Dictionary of lexeme name definitions keyed by number in base36</returns>
    public static Dictionary<string, LexemeNameDefinition> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new Dictionary<string, LexemeNameDefinition>();

        var definitions = new Dictionary<string, LexemeNameDefinition>();
        var lineNumber = 0;

        using var reader = new StreamReader(filePath);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            line = line.Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith("//"))
                continue;

            try
            {
                var definition = ParseLine(line);
                if (definition != null)
                {
                    var base36Key = ToBase36(definition.Number);
                    definitions[base36Key] = definition;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing definition line {lineNumber}: {line}", ex);
            }
        }

        return definitions;
    }

    /// <summary>
    /// Parses lexeme name definition lines and returns a dictionary by name
    /// </summary>
    /// <param name="lines">Lines to parse</param>
    /// <returns>Dictionary of lexeme name definitions keyed by name</returns>
    public static Dictionary<string, LexemeNameDefinition> ParseLines(string[] lines)
    {
        var definitions = new Dictionary<string, LexemeNameDefinition>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith("//"))
                continue;

            try
            {
                var definition = ParseLine(line);
                if (definition != null)
                {
                    definitions[definition.Name] = definition;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing definition line {i + 1}: {line}", ex);
            }
        }

        return definitions;
    }

    /// <summary>
    /// Parses a single lexeme name definition line
    /// </summary>
    /// <param name="line">Line to parse</param>
    /// <returns>Parsed lexeme name definition or null if line doesn't match pattern</returns>
    public static LexemeNameDefinition? ParseLine(string line)
    {
        var match = DefinitionPattern.Match(line);
        if (!match.Success)
            return null;

        // Extract name (either quoted or unquoted)
        var name = !string.IsNullOrEmpty(match.Groups[1].Value) 
            ? match.Groups[1].Value  // Quoted name
            : match.Groups[2].Value; // Unquoted name

        // Extract hex number and convert to decimal
        var hexNumber = match.Groups[3].Value;
        var number = Convert.ToInt64(hexNumber, 16);

        // Extract optional data type
        var dataType = match.Groups[4].Success && !string.IsNullOrWhiteSpace(match.Groups[4].Value)
            ? match.Groups[4].Value.Trim()
            : null;

        return new LexemeNameDefinition(name, number, dataType);
    }

    /// <summary>
    /// Creates a dictionary by number for fast lookup during lexeme parsing
    /// </summary>
    /// <param name="definitions">Dictionary of definitions by name</param>
    /// <returns>Dictionary of definitions by number</returns>
    public static Dictionary<long, LexemeNameDefinition> CreateNumberLookup(
        Dictionary<string, LexemeNameDefinition> definitions)
    {
        return definitions.Values.ToDictionary(def => def.Number, def => def);
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
            if (File.Exists(candidatePath))
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
    private static string ToBase36(long value)
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
