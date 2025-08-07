using System.Text;
using LexemeExtractor.Models;
using LexemeExtractor.OutputFormatters;

/// <summary>
/// Test program to verify AOT compatibility of streaming formatters
/// </summary>
class TestAotCompatibility
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Testing AOT-Compatible Streaming Formatters ===");
        Console.WriteLine();

        // Create test data
        var header = new FileHeader("C~~1.0", "test.c", "UTF-8");
        var lexemes = new[]
        {
            new Lexeme("k", "1", new Position(1, 1), LexemeContentFactory.String("hello \"world\"")),
            new Lexeme("2v", "2", new Position(1, 15), LexemeContentFactory.Number(-42)),
            new Lexeme("b", "3", new Position(2, 1), LexemeContentFactory.Boolean(true)),
            new Lexeme("0", "4", new Position(2, 5), LexemeContent.Empty),
            new Lexeme("k", "5", new Position(3, 1), LexemeContentFactory.String("line\nbreak\ttab"))
        };

        // Test JSON formatter (the one with AOT issues)
        Console.WriteLine("Testing JSON formatter:");
        Console.WriteLine(new string('-', 30));
        
        try
        {
            var output = new StringBuilder();
            using var writer = new StringWriter(output);
            using var formatter = new JsonStreamingFormatter(writer);

            formatter.WriteHeader(header);
            foreach (var lexeme in lexemes)
            {
                formatter.WriteLexeme(lexeme);
            }
            formatter.WriteFooter(lexemes.Length);

            var result = output.ToString();
            Console.WriteLine(result);
            
            // Validate JSON structure
            if (!result.StartsWith("{") || !result.EndsWith("}"))
                throw new Exception("Invalid JSON structure");
            
            if (!result.Contains("\"Domain\": \"C~~1.0\""))
                throw new Exception("Missing domain in JSON");
                
            if (!result.Contains("\"LexemeCount\": 5"))
                throw new Exception("Missing lexeme count in JSON");
                
            if (!result.Contains("\"hello \\\"world\\\"\""))
                throw new Exception("JSON string escaping failed");
                
            if (!result.Contains("\"line\\nbreak\\ttab\""))
                throw new Exception("JSON control character escaping failed");

            Console.WriteLine("✅ JSON formatter AOT compatibility test passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ JSON formatter test failed: {ex.Message}");
            Environment.Exit(1);
        }

        Console.WriteLine();
        Console.WriteLine("🎉 AOT compatibility tests completed successfully!");
    }
}
