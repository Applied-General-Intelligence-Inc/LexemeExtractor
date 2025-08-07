
using System.Text;
using LexemeExtractor.Models;
using LexemeExtractor.OutputFormatters;
using LexemeExtractor.Parsing;

// Parse command line arguments
var (format, globPattern, useStdin) = ParseArguments(args);

if (globPattern == null && !useStdin)
{
    Console.WriteLine("Usage: LexemeExtractor [--format <format>] <glob-pattern>");
    Console.WriteLine("       LexemeExtractor [--format <format>] < input.lexemes");
    Console.WriteLine($"Formats: {string.Join(", ", FormatterFactory.GetSupportedFormats())} (default: text)");
    Console.WriteLine("Example: LexemeExtractor --format json \"*.lexemes\"");
    Console.WriteLine("Example: LexemeExtractor \"*.lexemes\"");
    Console.WriteLine("Example: cat file.lexemes | LexemeExtractor --format json");
    return 1;
}

try
{
    if (useStdin)
    {
        // Process input from stdin and output to stdout
        ProcessStdin(format);
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

        // Process each matching file using modern foreach
        foreach (var filePath in matchingFiles)
        {
            var outputFilePath = GenerateOutputFileName(filePath, format);
            ProcessFile(filePath, outputFilePath, format);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error processing files: {ex.Message}");
    return 1;
}

return 0;

static void ProcessFile(string inputFilePath, string outputFilePath, string format)
{
    Console.WriteLine($"Processing file: {Path.GetFileName(inputFilePath)} -> {Path.GetFileName(outputFilePath)}");

    try
    {
        using var outputWriter = new StreamWriter(outputFilePath);
        using var formatter = FormatterFactory.CreateFormatter(format, outputWriter);

        ProcessFileWithStreaming(inputFilePath, formatter);

        Console.WriteLine($"Successfully processed file with streaming formatter");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing file: {ex.Message}");
    }
}

static void ProcessFileWithStreaming(string filePath, ILexemeFormatter formatter)
{
    using var reader = new StreamReader(filePath);

    // Parse header - real format has 3 lines: domain, filename, encoding
    var domain = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing domain line");
    var filename = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing filename line");
    var encoding = reader.ReadLine()?.Trim() ?? "UTF-8";
    var header = new FileHeader(domain, filename, encoding);

    formatter.WriteHeader(header);

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
            var lexeme = LexemeFileParser.ParseLexemeLine(line, positionDecoder);
            formatter.WriteLexeme(lexeme);
            count++;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing line {lineNumber}: {line}", ex);
        }

        lineNumber++;
    }

    formatter.WriteFooter(count);
}

static void ProcessStdin(string format)
{
    Console.Error.WriteLine($"Processing stdin with format: {format}");

    try
    {
        using var formatter = FormatterFactory.CreateFormatter(format, Console.Out);
        ProcessStreamWithStreaming(Console.In, formatter);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error processing stdin: {ex.Message}");
        Environment.Exit(1);
    }
}

static void ProcessStreamWithStreaming(TextReader reader, ILexemeFormatter formatter)
{
    // Parse header - real format has 3 lines: domain, filename, encoding
    var domain = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing domain line");
    var filename = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing filename line");
    var encoding = reader.ReadLine()?.Trim() ?? "UTF-8";
    var header = new FileHeader(domain, filename, encoding);

    formatter.WriteHeader(header);

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
            var lexeme = LexemeFileParser.ParseLexemeLine(line, positionDecoder);
            formatter.WriteLexeme(lexeme);
            count++;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing line {lineNumber}: {line}", ex);
        }

        lineNumber++;
    }

    formatter.WriteFooter(count);
}

static (string format, string? globPattern, bool useStdin) ParseArguments(string[] args)
{
    var format = "text"; // default format for files
    string? globPattern = null;
    bool useStdin = false;

    // Check if stdin has data available (piped input)
    if (!Console.IsInputRedirected && args.Length == 0)
    {
        // No arguments and no piped input - show usage
        return (format, null, false);
    }

    if (Console.IsInputRedirected || (args.Length > 0 && args.All(arg => arg.StartsWith("--"))))
    {
        // Either input is redirected, or all arguments are options (no glob pattern)
        useStdin = true;
    }

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--format" && i + 1 < args.Length)
        {
            format = args[i + 1].ToLowerInvariant();
            var supportedFormats = FormatterFactory.GetSupportedFormats();
            if (!supportedFormats.Contains(format))
            {
                Console.WriteLine($"Invalid format: {format}. Valid formats are: {string.Join(", ", supportedFormats)}");
                return (format, null, false);
            }
            i++; // skip the format value
        }
        else if (!args[i].StartsWith("--"))
        {
            globPattern = args[i];
            useStdin = false; // If we have a glob pattern, don't use stdin
        }
    }

    return (format, globPattern, useStdin);
}

static string GenerateOutputFileName(string inputFilePath, string format)
{
    var directory = Path.GetDirectoryName(inputFilePath) ?? ".";
    var fileName = Path.GetFileName(inputFilePath);
    var extension = FormatterFactory.GetFileExtension(format);

    return Path.Combine(directory, $"{fileName}{extension}");
}


