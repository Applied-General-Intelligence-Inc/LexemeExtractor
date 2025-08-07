using System.Text;
using LexemeExtractor.ManualParsing;
using LexemeExtractor.Models;

namespace LexemeExtractor.Tests;

/// <summary>
/// Test to analyze and identify column offset issues by comparing extracted lexemes 
/// with the original COBOL source file
/// </summary>
public class ColumnOffsetAnalysisTest
{
    /// <summary>
    /// Analyzes the column offset issue by comparing lexemes with original source
    /// </summary>
    public static void AnalyzeColumnOffsets()
    {
        Console.WriteLine("=== Column Offset Analysis ===");
        Console.WriteLine();

        var lexemeFilePath = "SampleInput/SRT1000.CBL.lexemes";
        var sourceFilePath = "SampleInput/SRT1000.CBL";

        if (!File.Exists(lexemeFilePath) || !File.Exists(sourceFilePath))
        {
            Console.WriteLine($"Required files not found:");
            Console.WriteLine($"  Lexeme file: {lexemeFilePath} (exists: {File.Exists(lexemeFilePath)})");
            Console.WriteLine($"  Source file: {sourceFilePath} (exists: {File.Exists(sourceFilePath)})");
            return;
        }

        try
        {
            // Load the original source file
            var sourceLines = File.ReadAllLines(sourceFilePath);
            Console.WriteLine($"Loaded {sourceLines.Length} lines from source file");

            // Parse the lexeme file
            var lexemes = ParseLexemeFile(lexemeFilePath);
            Console.WriteLine($"Parsed {lexemes.Count} lexemes from lexeme file");
            Console.WriteLine();

            // Analyze lexemes with string content (these should match source exactly)
            AnalyzeStringLexemes(lexemes, sourceLines);

            Console.WriteLine();
            Console.WriteLine("=== Analysis Complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during analysis: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Parse the lexeme file and return all lexemes
    /// </summary>
    private static List<Lexeme> ParseLexemeFile(string filePath)
    {
        var lexemes = new List<Lexeme>();
        
        using var reader = new StreamReader(filePath);
        
        // Skip header lines
        reader.ReadLine(); // domain
        reader.ReadLine(); // filename  
        reader.ReadLine(); // encoding

        // Load lexeme name definitions if available
        var domain = "COBOL~IBMEnterprise"; // From the sample file
        var definitionFilePath = LexemeNameDefinitionParser.GetDefinitionFilePath(domain, filePath);
        var nameDefinitions = LexemeNameDefinitionParser.ParseFile(definitionFilePath);

        var positionDecoder = new PositionDecoder();
        var lineNumber = 4;

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
                var lexeme = LexemeFileParser.ParseLexemeLine(line, positionDecoder, nameDefinitions);
                lexemes.Add(lexeme);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing line {lineNumber}: {line}");
                Console.WriteLine($"  Error: {ex.Message}");
            }

            lineNumber++;
        }

        return lexemes;
    }

    /// <summary>
    /// Analyze lexemes that contain string content to check column alignment
    /// </summary>
    private static void AnalyzeStringLexemes(List<Lexeme> lexemes, string[] sourceLines)
    {
        Console.WriteLine("=== Analyzing String Lexemes ===");
        
        var stringLexemes = lexemes
            .Where(l => l.Content is LexemeExtractor.Models.StringContent)
            .Take(20) // Analyze first 20 string lexemes
            .ToList();

        Console.WriteLine($"Found {stringLexemes.Count} string lexemes to analyze (showing first 20)");
        Console.WriteLine();

        foreach (var lexeme in stringLexemes)
        {
            AnalyzeSingleStringLexeme(lexeme, sourceLines);
        }
    }

    /// <summary>
    /// Analyze a single string lexeme against the source
    /// </summary>
    private static void AnalyzeSingleStringLexeme(Lexeme lexeme, string[] sourceLines)
    {
        var stringContent = (LexemeExtractor.Models.StringContent)lexeme.Content;
        var expectedString = stringContent.StringValue;
        var line = lexeme.Position.Line;
        var column = lexeme.Position.Column;

        Console.WriteLine($"Lexeme: \"{expectedString}\" at Line {line}, Column {column}");

        // Check if line number is valid
        if (line < 1 || line > sourceLines.Length)
        {
            Console.WriteLine($"  ERROR: Line {line} is out of range (1-{sourceLines.Length})");
            Console.WriteLine();
            return;
        }

        var sourceLine = sourceLines[line - 1]; // Convert to 0-based index
        Console.WriteLine($"  Source line: \"{sourceLine}\"");

        // Check if column is valid
        if (column < 1 || column > sourceLine.Length + 1)
        {
            Console.WriteLine($"  ERROR: Column {column} is out of range (1-{sourceLine.Length + 1})");
            Console.WriteLine();
            return;
        }

        // Check exact match at reported position
        var actualAtPosition = ExtractStringAtPosition(sourceLine, column, expectedString.Length);
        var isExactMatch = actualAtPosition == expectedString;

        Console.WriteLine($"  At reported position: \"{actualAtPosition}\" (Match: {isExactMatch})");

        if (!isExactMatch)
        {
            // Check nearby positions to find the actual location
            var foundPosition = FindStringInLine(sourceLine, expectedString);
            if (foundPosition != -1)
            {
                var actualColumn = foundPosition + 1; // Convert to 1-based
                var offset = actualColumn - column;
                Console.WriteLine($"  Found at column {actualColumn} (offset: {offset:+0;-0;0})");
            }
            else
            {
                Console.WriteLine($"  String not found in line");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Extract a substring from a specific position in a line
    /// </summary>
    private static string ExtractStringAtPosition(string line, int column, int length)
    {
        var startIndex = column - 1; // Convert to 0-based index
        
        if (startIndex < 0 || startIndex >= line.Length)
            return "";

        var endIndex = Math.Min(startIndex + length, line.Length);
        return line[startIndex..endIndex];
    }

    /// <summary>
    /// Find the position of a string within a line
    /// </summary>
    private static int FindStringInLine(string line, string searchString)
    {
        return line.IndexOf(searchString, StringComparison.Ordinal);
    }

    /// <summary>
    /// Run the column offset analysis
    /// </summary>
    public static void RunAnalysis()
    {
        try
        {
            AnalyzeColumnOffsets();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Analysis failed: {ex.Message}");
            throw;
        }
    }
}
