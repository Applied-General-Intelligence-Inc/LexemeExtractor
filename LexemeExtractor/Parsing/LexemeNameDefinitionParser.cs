using System.Text.RegularExpressions;
using LexemeExtractor.Models;

namespace LexemeExtractor.Parsing;

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
    /// Parses a lexeme name definition file and returns a dictionary by name
    /// </summary>
    /// <param name="filePath">Path to the definition file</param>
    /// <returns>Dictionary of lexeme name definitions keyed by name</returns>
    public static Dictionary<string, LexemeNameDefinition> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new Dictionary<string, LexemeNameDefinition>();

        var lines = File.ReadAllLines(filePath);
        return ParseLines(lines);
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
    /// Gets the expected definition file path for a given domain and lexeme file path
    /// </summary>
    /// <param name="domain">Domain from the lexeme file header</param>
    /// <param name="lexemeFilePath">Path to the lexeme file</param>
    /// <returns>Expected path to the definition file</returns>
    public static string GetDefinitionFilePath(string domain, string lexemeFilePath)
    {
        var directory = Path.GetDirectoryName(lexemeFilePath) ?? "";
        var domainFileName = $"{domain}.txt";
        return Path.Combine(directory, domainFileName);
    }
}
