
using System.Text;
using LexemeExtractor.Superpower;
using LexemeExtractor.Models;
using static LexemeExtractor.Superpower.CompressedLexemeParser;

// Parse command line arguments
var (globPattern, useStdin) = ParseArguments(args);

if (globPattern == null && !useStdin)
{
    Console.WriteLine("Usage: LexemeExtractor <glob-pattern>");
    Console.WriteLine("       LexemeExtractor < input.lexemes");
    Console.WriteLine("Example: LexemeExtractor \"*.lexemes\"");
    Console.WriteLine("Example: cat file.lexemes | LexemeExtractor");
    return 1;
}

try
{
    if (useStdin)
    {
        // Process input from stdin
        ProcessStdin();
    }
    else
    {
        // Expand ~ to home directory if present using pattern matching
        var expandedPattern = globPattern switch
        {
            ['~', '/', .. var rest] => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), rest),
            _ => globPattern
        };

        // Separate directory path from pattern
        var directory = Path.GetDirectoryName(expandedPattern) switch
        {
            null or "" => ".",
            var dir => dir
        };
        var pattern = Path.GetFileName(expandedPattern) ?? "*";

        // Get all files matching the glob pattern
        var matchingFiles = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);

        if (matchingFiles.Length == 0)
        {
            Console.WriteLine($"No files found matching pattern: {globPattern}");
            return 0;
        }

        // Process each matching file
        foreach (var filePath in matchingFiles)
        {
            ProcessFile(filePath);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error processing files: {ex.Message}");
    return 1;
}

return 0;

static void ProcessFile(string inputFilePath)
{
    Console.WriteLine($"Processing file: {Path.GetFileName(inputFilePath)}");

    try
    {
        var fileContent = System.IO.File.ReadAllText(inputFilePath);
        var parsedFile = Parse(fileContent);

        Console.WriteLine($"Successfully parsed file:");
        Console.WriteLine($"  Domain: {parsedFile.Domain}");
        Console.WriteLine($"  File Source: {parsedFile.FileSourceInformation}");
        Console.WriteLine($"  Encoding: {parsedFile.Encoding}");
        Console.WriteLine($"  Lexemes: {parsedFile.Lexemes.Count}");

        // Display first few lexemes for verification
        var lexemesToShow = Math.Min(5, parsedFile.Lexemes.Count);
        for (int i = 0; i < lexemesToShow; i++)
        {
            var lexeme = parsedFile.Lexemes[i];
            Console.WriteLine($"  [{i}] Type: {lexeme.Type}, Number: {lexeme.Radix36Number}, Position: {lexeme.Position.GetType().Name}, Content: {lexeme.Content.GetType().Name}");
        }

        if (parsedFile.Lexemes.Count > lexemesToShow)
        {
            Console.WriteLine($"  ... and {parsedFile.Lexemes.Count - lexemesToShow} more lexemes");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing file: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }
}

static void ProcessStdin()
{
    Console.Error.WriteLine("Processing stdin");

    try
    {
        var input = Console.In.ReadToEnd();
        var parsedFile = Parse(input);

        Console.WriteLine($"Successfully parsed stdin:");
        Console.WriteLine($"  Domain: {parsedFile.Domain}");
        Console.WriteLine($"  File Source: {parsedFile.FileSourceInformation}");
        Console.WriteLine($"  Encoding: {parsedFile.Encoding}");
        Console.WriteLine($"  Lexemes: {parsedFile.Lexemes.Count}");

        // Display first few lexemes for verification
        var lexemesToShow = Math.Min(5, parsedFile.Lexemes.Count);
        for (int i = 0; i < lexemesToShow; i++)
        {
            var lexeme = parsedFile.Lexemes[i];
            Console.WriteLine($"  [{i}] Type: {lexeme.Type}, Number: {lexeme.Radix36Number}, Position: {lexeme.Position.GetType().Name}, Content: {lexeme.Content.GetType().Name}");
        }

        if (parsedFile.Lexemes.Count > lexemesToShow)
        {
            Console.WriteLine($"  ... and {parsedFile.Lexemes.Count - lexemesToShow} more lexemes");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error processing stdin: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        Environment.Exit(1);
    }
}

static (string? globPattern, bool useStdin) ParseArguments(string[] args)
{
    string? globPattern = null;
    bool useStdin = false;

    // Check if stdin has data available (piped input)
    if (!Console.IsInputRedirected && args.Length == 0)
    {
        // No arguments and no piped input - show usage
        return (null, false);
    }

    if (Console.IsInputRedirected || args.Length == 0)
    {
        // Either input is redirected, or no arguments provided
        useStdin = true;
    }
    else
    {
        // Use the first argument as the glob pattern
        globPattern = args[0];
        useStdin = false;
    }

    return (globPattern, useStdin);
}




