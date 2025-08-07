// Column analysis runner - standalone program to analyze column offsets
using System;
using System.IO;
using System.Diagnostics;

Console.WriteLine("=== Column Offset Analysis ===");
Console.WriteLine();

// Change to LexemeExtractor directory
var currentDir = Directory.GetCurrentDirectory();
var lexemeExtractorDir = Path.Combine(currentDir, "LexemeExtractor");

if (Directory.Exists(lexemeExtractorDir))
{
    Directory.SetCurrentDirectory(lexemeExtractorDir);
    Console.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
}
else
{
    Console.WriteLine("LexemeExtractor directory not found!");
    return 1;
}

// Create a test file with first 50 lexemes to avoid parsing errors
Console.WriteLine("Creating test file with first 50 lexemes...");
try
{
    var lines = File.ReadAllLines("SampleInput/SRT1000.CBL.lexemes");
    var testLines = new string[Math.Min(53, lines.Length)]; // 3 header + 50 lexemes
    Array.Copy(lines, testLines, testLines.Length);
    File.WriteAllLines("test_analysis.lexemes", testLines);
    Console.WriteLine($"Created test file with {testLines.Length - 3} lexemes");
}
catch (Exception ex)
{
    Console.WriteLine($"Error creating test file: {ex.Message}");
    return 1;
}

// Run lexeme extractor
Console.WriteLine("\nRunning lexeme extractor...");
try
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "/home/chaz/.dotnet/dotnet",
            Arguments = "run -- --format text test_analysis.lexemes",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Directory.GetCurrentDirectory()
        }
    };

    process.Start();
    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        Console.WriteLine($"Error running extractor: {error}");
        return 1;
    }

    Console.WriteLine("Lexeme extraction completed successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to run extractor: {ex.Message}");
    return 1;
}

// Analyze the output
Console.WriteLine("\nAnalyzing output...");
if (!File.Exists("test_analysis.lexemes.txt"))
{
    Console.WriteLine("Output file not found!");
    return 1;
}

var extractorOutput = File.ReadAllText("test_analysis.lexemes.txt");
var sourceLines = File.ReadAllLines("SampleInput/SRT1000.CBL");

Console.WriteLine("\nLooking for string lexemes to verify column positions...");

// Parse the output to find string lexemes
var outputLines = extractorOutput.Split('\n');
var stringLexemes = new List<(int line, int startCol, int endCol, string value)>();

foreach (var line in outputLines)
{
    if (line.Contains("= \"") && line.Contains("@ Line"))
    {
        try
        {
            // Parse format: #ep(529) [D] @ Line 3, Column 21-28 = "SRT1000"
            var parts = line.Split(new[] { "@ Line ", ", Column ", " = \"" }, StringSplitOptions.None);
            if (parts.Length >= 4)
            {
                var lineNum = int.Parse(parts[1]);
                var columnPart = parts[2];
                var value = parts[3].TrimEnd('"');

                if (columnPart.Contains('-'))
                {
                    var colParts = columnPart.Split('-');
                    var startCol = int.Parse(colParts[0]);
                    var endCol = int.Parse(colParts[1]);
                    stringLexemes.Add((lineNum, startCol, endCol, value));
                }
            }
        }
        catch
        {
            // Skip malformed lines
        }
    }
}

Console.WriteLine($"Found {stringLexemes.Count} string lexemes to analyze");
Console.WriteLine();

// Analyze each string lexeme
foreach (var (lineNum, startCol, endCol, expectedValue) in stringLexemes.Take(10))
{
    Console.WriteLine($"Analyzing: \"{expectedValue}\" at Line {lineNum}, Column {startCol}-{endCol}");
    
    if (lineNum < 1 || lineNum > sourceLines.Length)
    {
        Console.WriteLine($"  ERROR: Line {lineNum} out of range");
        continue;
    }

    var sourceLine = sourceLines[lineNum - 1];
    Console.WriteLine($"  Source line: \"{sourceLine}\"");

    // Check if the reported position is correct
    if (startCol < 1 || startCol > sourceLine.Length)
    {
        Console.WriteLine($"  ERROR: Start column {startCol} out of range");
        continue;
    }

    var actualLength = Math.Min(expectedValue.Length, sourceLine.Length - startCol + 1);
    var actualAtPosition = sourceLine.Substring(startCol - 1, actualLength);
    
    Console.WriteLine($"  At reported position: \"{actualAtPosition}\"");
    Console.WriteLine($"  Match: {actualAtPosition == expectedValue}");

    if (actualAtPosition != expectedValue)
    {
        // Find where it actually appears
        var actualIndex = sourceLine.IndexOf(expectedValue);
        if (actualIndex >= 0)
        {
            var actualColumn = actualIndex + 1;
            var offset = actualColumn - startCol;
            Console.WriteLine($"  Actually found at column {actualColumn} (offset: {offset:+0;-0;0})");
        }
        else
        {
            Console.WriteLine($"  String not found in line!");
        }
    }
    
    Console.WriteLine();
}

Console.WriteLine("Analysis complete!");
return 0;
