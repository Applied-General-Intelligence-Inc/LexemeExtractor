

// Parse command line arguments
var (format, globPattern, useStdin) = ParseArguments(args);

if (globPattern == null && !useStdin)
{
    Console.WriteLine("Usage: LexemeExtractor [--format <format>] <glob-pattern>");
    Console.WriteLine("       LexemeExtractor [--format <format>] < input.lexemes");
    Console.WriteLine("Formats: text (default for files), json, csv");
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
            ProcessFile(filePath, outputFilePath);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error processing files: {ex.Message}");
    return 1;
}

return 0;

static void ProcessFile(string inputFilePath, string outputFilePath)
{
    Console.WriteLine($"Processing file: {Path.GetFileName(inputFilePath)} -> {Path.GetFileName(outputFilePath)}");
    // TODO: Implement actual lexeme processing and output formatting
}

static void ProcessStdin(string format)
{
    Console.Error.WriteLine($"Processing stdin with format: {format}");
    // TODO: Read from stdin, process lexeme data, and write to stdout
    // For now, just echo the input to demonstrate stdin/stdout functionality
    string? line;
    while ((line = Console.ReadLine()) != null)
    {
        Console.WriteLine($"[{format}] {line}");
    }
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
            if (format != "text" && format != "json" && format != "csv")
            {
                Console.WriteLine($"Invalid format: {format}. Valid formats are: text, json, csv");
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

    var extension = format switch
    {
        "json" => ".json",
        "csv" => ".csv",
        _ => ".txt" // text format outputs to .txt file
    };

    return Path.Combine(directory, $"{fileName}{extension}");
}
