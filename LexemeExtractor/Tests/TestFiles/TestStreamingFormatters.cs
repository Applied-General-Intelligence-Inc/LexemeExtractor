using LexemeExtractor.Tests;

/// <summary>
/// Simple test runner for streaming formatters
/// </summary>
class TestStreamingFormatters
{
    static void Main(string[] args)
    {
        try
        {
            StreamingIntegrationTest.RunAllTests();
            Console.WriteLine("\n✅ All tests passed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Tests failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
