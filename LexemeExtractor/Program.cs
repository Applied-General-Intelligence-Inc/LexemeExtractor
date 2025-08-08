
using System.Text;
using LexemeExtractor.Superpower;
using LexemeExtractor.Models;
using LexemeExtractor.OutputFormatters;
using static LexemeExtractor.Superpower.CompressedLexemeParser;

// Parse command line arguments
var parseResult = ParseArguments(args);

// Handle special cases first
if (parseResult.ShowHelp)
{
    ShowHelp();
    return 0;
}

if (parseResult.ShowVersion)
{
    ShowVersion();
    return 0;
}

if (parseResult.HasError)
{
    Console.Error.WriteLine($"Error: {parseResult.ErrorMessage}");
    Console.Error.WriteLine("Use --help for usage information.");
    return 1;
}

if (parseResult.GlobPattern == null && !parseResult.UseStdin)
{
    Console.Error.WriteLine("Error: No input specified.");
    Console.Error.WriteLine("Use --help for usage information.");
    return 1;
}

try
{
    if (parseResult.UseStdin)
    {
        // Process input from stdin
        ProcessStdin(parseResult.OutputFormat);
    }
    else
    {
        // Expand ~ to home directory if present using pattern matching
        var expandedPattern = parseResult.GlobPattern switch
        {
            ['~', '/', .. var rest] => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), rest),
            _ => parseResult.GlobPattern
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
            Console.WriteLine($"No files found matching pattern: {parseResult.GlobPattern}");
            return 0;
        }

        // Process each matching file
        foreach (var filePath in matchingFiles)
        {
            ProcessFile(filePath, parseResult.OutputFormat);
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
        using var fileReader = new StreamReader(inputFilePath);

        // Parse header and get domain for name definitions
        var header = CompressedLexemeParser.ParseHeader(fileReader);
        var domain = header.Domain;

        // Load name definitions using Superpower parser
        var definitionFilePath = NameDefinitionParser.GetDefinitionFilePath(domain, inputFilePath);
        var nameDefinitions = NameDefinitionParser.ParseFile(definitionFilePath);

        // Create output file path by appending extension
        var outputExtension = FormatterFactory.GetFileExtension(outputFormat);
        var outputFilePath = inputFilePath + outputExtension;

        // Use the formatter to output the results to file
        using var outputWriter = new StreamWriter(outputFilePath);
        using var formatter = FormatterFactory.CreateFormatter(outputFormat, outputWriter);

        // Write header
        formatter.WriteHeader(header);

        // Stream lexemes one at a time directly from file
        var lexemeCount = 0;
        foreach (var lexeme in CompressedLexemeParser.ParseLexemes(fileReader, nameDefinitions))
        {
            formatter.WriteLexeme(lexeme);
            lexemeCount++;
        }
        formatter.WriteFooter(lexemeCount);

        Console.WriteLine($"Output written to: {outputFilePath}");
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
        // Parse header and get domain for name definitions
        var header = CompressedLexemeParser.ParseHeader(Console.In);
        var domain = header.Domain;

        // Load name definitions using Superpower parser
        var definitionFilePath = NameDefinitionParser.GetDefinitionFilePath(domain, ".");
        var nameDefinitions = NameDefinitionParser.ParseFile(definitionFilePath);

        // Use the formatter to output the results
        using var formatter = FormatterFactory.CreateFormatter(outputFormat, Console.Out);

        // Write header
        formatter.WriteHeader(header);

        // Stream lexemes one at a time directly from stdin
        var lexemeCount = 0;
        foreach (var lexeme in CompressedLexemeParser.ParseLexemes(Console.In, nameDefinitions))
        {
            formatter.WriteLexeme(lexeme);
            lexemeCount++;
        }
        formatter.WriteFooter(lexemeCount);
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

static void ShowHelp()
{
    Console.WriteLine("LexemeExtractor - Process lexeme files and convert to various formats");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("    LexemeExtractor <glob-pattern> [OPTIONS]");
    Console.WriteLine("    LexemeExtractor [OPTIONS] < input.lexemes");
    Console.WriteLine();
    Console.WriteLine("ARGUMENTS:");
    Console.WriteLine("    <glob-pattern>    File pattern to match (e.g., \"*.lexemes\", \"data/*.lex\")");
    Console.WriteLine();
    Console.WriteLine("OPTIONS:");
    Console.WriteLine("    --format <format>    Output format: text, json, csv, xml (default: text)");
    Console.WriteLine("    --help, -h           Show this help message");
    Console.WriteLine("    --version, -v        Show version information");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("    LexemeExtractor \"*.lexemes\"");
    Console.WriteLine("    LexemeExtractor \"data/*.lex\" --format json");
    Console.WriteLine("    cat file.lexemes | LexemeExtractor --format csv");
    Console.WriteLine("    LexemeExtractor ~/documents/*.lexemes --format xml");
    Console.WriteLine();
    Console.WriteLine("NOTES:");
    Console.WriteLine("    - Output files are created with the same name as input plus format extension");
    Console.WriteLine("    - Lexeme name definitions are automatically loaded from companion .txt files");
    Console.WriteLine("    - Supports piped input for processing single files");
}

static void ShowVersion()
{
    var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
    Console.WriteLine($"LexemeExtractor {version}");
    Console.WriteLine("A .NET 9.0 AOT compiled application for processing lexeme files");
}

static ParseResult ParseArguments(string[] args)
{
    var result = new ParseResult();

    // Parse arguments
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        switch (arg)
        {
            case "--help" or "-h":
                result.ShowHelp = true;
                return result;

            case "--version" or "-v":
                result.ShowVersion = true;
                return result;

            case "--format":
                if (i + 1 >= args.Length)
                {
                    result.HasError = true;
                    result.ErrorMessage = "--format requires a value";
                    return result;
                }
                result.OutputFormat = args[i + 1].ToLowerInvariant();
                i++; // Skip the format value
                break;

            default:
                if (arg.StartsWith("--"))
                {
                    result.HasError = true;
                    result.ErrorMessage = $"Unknown option: {arg}";
                    return result;
                }
                else if (result.GlobPattern == null)
                {
                    result.GlobPattern = arg; // First non-option argument is the glob pattern
                }
                else
                {
                    result.HasError = true;
                    result.ErrorMessage = $"Unexpected argument: {arg}";
                    return result;
                }
                break;
        }
    }

    // Validate format
    var validFormats = new[] { "text", "json", "csv", "xml" };
    if (!validFormats.Contains(result.OutputFormat))
    {
        result.HasError = true;
        result.ErrorMessage = $"Invalid format '{result.OutputFormat}'. Valid formats: {string.Join(", ", validFormats)}";
        return result;
    }

    // Determine if we should use stdin
    if (Console.IsInputRedirected || (args.Length > 0 && result.GlobPattern == null && !result.ShowHelp && !result.ShowVersion))
    {
        result.UseStdin = true;
    }

    return result;
}

record ParseResult
{
    public string? GlobPattern { get; set; }
    public string OutputFormat { get; set; } = "text";
    public bool UseStdin { get; set; }
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}




