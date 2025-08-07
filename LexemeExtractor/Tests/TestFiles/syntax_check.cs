// Simple syntax check for the lexeme name definition functionality
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Minimal versions of the classes to check syntax
namespace LexemeExtractor.Models
{
    public record LexemeNameDefinition
    {
        public string Name { get; init; } = string.Empty;
        public long Number { get; init; }
        public string? DataType { get; init; }
        
        public LexemeNameDefinition(string name, long number, string? dataType = null)
        {
            Name = name;
            Number = number;
            DataType = dataType;
        }
        
        public LexemeNameDefinition() { }
    }
}

namespace LexemeExtractor.Parsing
{
    using LexemeExtractor.Models;
    
    public static class LexemeNameDefinitionParser
    {
        private static readonly Regex DefinitionPattern = new(
            @"^(?:'([^']+)'|([^=\s]+))\s*=\s*:([0-9A-Fa-f]+)(?:\s+([^;]+))?\s*;?\s*$",
            RegexOptions.Compiled);

        public static Dictionary<string, LexemeNameDefinition> ParseFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return new Dictionary<string, LexemeNameDefinition>();

            var lines = System.IO.File.ReadAllLines(filePath);
            return ParseLines(lines);
        }

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

        public static Dictionary<long, LexemeNameDefinition> CreateNumberLookup(
            Dictionary<string, LexemeNameDefinition> definitions)
        {
            return definitions.Values.ToDictionary(def => def.Number, def => def);
        }

        public static string GetDefinitionFilePath(string domain, string lexemeFilePath)
        {
            var directory = System.IO.Path.GetDirectoryName(lexemeFilePath) ?? "";
            var domainFileName = $"{domain}.txt";
            return System.IO.Path.Combine(directory, domainFileName);
        }
    }
}

// Test the syntax
class Program
{
    static void Main()
    {
        Console.WriteLine("Syntax check passed!");
    }
}
