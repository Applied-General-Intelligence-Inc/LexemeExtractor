// Simple test runner for column offset analysis
using LexemeExtractor.Tests;

Console.WriteLine("Column Offset Analysis Test Runner");
Console.WriteLine("==================================");
Console.WriteLine();

try
{
    // Change to the LexemeExtractor directory to find the sample files
    var currentDir = Directory.GetCurrentDirectory();
    var lexemeExtractorDir = Path.Combine(currentDir, "LexemeExtractor");
    
    if (Directory.Exists(lexemeExtractorDir))
    {
        Directory.SetCurrentDirectory(lexemeExtractorDir);
        Console.WriteLine($"Changed directory to: {Directory.GetCurrentDirectory()}");
        Console.WriteLine();
    }
    
    ColumnOffsetAnalysisTest.RunAnalysis();
}
catch (Exception ex)
{
    Console.WriteLine($"Test runner failed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    return 1;
}

return 0;
