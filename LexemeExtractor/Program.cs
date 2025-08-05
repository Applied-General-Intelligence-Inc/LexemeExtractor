using System.IO;

// Check if a glob pattern was provided as an argument
if (args.Length == 0)
{
    Console.WriteLine("Usage: LexemeExtractor <glob-pattern>");
    Console.WriteLine("Example: LexemeExtractor \"*.txt\"");
    return 1;
}

var globPattern = args[0];

try
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
    var pattern = Path.GetFileName(expandedPattern);
    
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
        ProcessFile(filePath);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error processing files: {ex.Message}");
    return 1;
}

return 0;

static void ProcessFile(string filePath) =>
    Console.WriteLine($"Processing file: {Path.GetFileName(filePath)}");
