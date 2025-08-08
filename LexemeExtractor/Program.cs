
using System.Text;
using LexemeExtractor.Superpower;
using LexemeExtractor.Models;
using LexemeExtractor.OutputFormatters;
using static LexemeExtractor.Superpower.CompressedLexemeParser;

// Parse command line arguments
var (globPattern, outputFormat, useStdin) = ParseArguments(args);

if (globPattern == null && !useStdin)
{
    Console.WriteLine("Usage: LexemeExtractor <glob-pattern> [--format <format>]");
    Console.WriteLine("       LexemeExtractor [--format <format>] < input.lexemes");
    Console.WriteLine("Formats: text (default), json, csv, xml");
    Console.WriteLine("Example: LexemeExtractor \"*.lexemes\" --format json");
    Console.WriteLine("Example: cat file.lexemes | LexemeExtractor --format csv");
    return 1;
}

try
{
    if (useStdin)
    {
        // Process input from stdin
        ProcessStdin(outputFormat);
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
            ProcessFile(filePath, outputFormat);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error processing files: {ex.Message}");
    return 1;
}

return 0;

static void ProcessFile(string inputFilePath, string outputFormat)
{
    try
    {
        var fileContent = System.IO.File.ReadAllText(inputFilePath);

        // Parse just the header to get the domain (without parsing all lexemes)
        var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var domain = lines.Length > 0 ? lines[0].Trim() : "";

        // Load name definitions using Superpower parser
        var definitionFilePath = NameDefinitionParser.GetDefinitionFilePath(domain, inputFilePath);
        var nameDefinitions = NameDefinitionParser.ParseFile(definitionFilePath);

        // Parse the file with name definitions
        var parsedFile = Parse(fileContent, nameDefinitions);

        // Use the formatter to output the results
        using var formatter = FormatterFactory.CreateFormatter(outputFormat, Console.Out);

        formatter.WriteHeader(parsedFile.Header);
        foreach (var lexeme in parsedFile.Lexemes)
        {
            formatter.WriteLexeme(lexeme);
        }
        formatter.WriteFooter(parsedFile.Lexemes.Count);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error processing file {Path.GetFileName(inputFilePath)}: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }
}

static void ProcessStdin(string outputFormat)
{
    try
    {
        var input = Console.In.ReadToEnd();

        // Parse just the header to get the domain (without parsing all lexemes)
        var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var domain = lines.Length > 0 ? lines[0].Trim() : "";

        // Load name definitions using Superpower parser
        var definitionFilePath = NameDefinitionParser.GetDefinitionFilePath(domain, "<stdin>");
        var nameDefinitions = NameDefinitionParser.ParseFile(definitionFilePath);

        // Parse the input with name definitions
        var parsedFile = Parse(input, nameDefinitions);

        // Use the formatter to output the results
        using var formatter = FormatterFactory.CreateFormatter(outputFormat, Console.Out);

        formatter.WriteHeader(parsedFile.Header);
        foreach (var lexeme in parsedFile.Lexemes)
        {
            formatter.WriteLexeme(lexeme);
        }
        formatter.WriteFooter(parsedFile.Lexemes.Count);
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

static (string? globPattern, string outputFormat, bool useStdin) ParseArguments(string[] args)
{
    string? globPattern = null;
    string outputFormat = "text"; // Default format
    bool useStdin = false;

    // Parse arguments
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--format" && i + 1 < args.Length)
        {
            outputFormat = args[i + 1].ToLowerInvariant();
            i++; // Skip the format value
        }
        else if (!args[i].StartsWith("--"))
        {
            globPattern = args[i]; // First non-option argument is the glob pattern
        }
    }

    // Check if stdin has data available (piped input)
    if (!Console.IsInputRedirected && args.Length == 0)
    {
        // No arguments and no piped input - show usage
        return (null, outputFormat, false);
    }

    if (Console.IsInputRedirected || (args.Length > 0 && globPattern == null))
    {
        // Either input is redirected, or only format options provided
        useStdin = true;
    }

    // Validate format
    var validFormats = new[] { "text", "json", "csv", "xml" };
    if (!validFormats.Contains(outputFormat))
    {
        Console.Error.WriteLine($"Invalid format '{outputFormat}'. Valid formats: {string.Join(", ", validFormats)}");
        outputFormat = "text";
    }

    return (globPattern, outputFormat, useStdin);
}




