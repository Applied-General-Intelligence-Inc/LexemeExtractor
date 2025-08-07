using System.Text;
using LexemeExtractor.Models;
using LexemeExtractor.OutputFormatters;

/// <summary>
/// Demonstration program for streaming formatters
/// </summary>
class DemoStreamingFormatters
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Streaming Output Formatters Demo ===");
        Console.WriteLine();

        // Create test data
        var header = new FileHeader("C~~1.0", "demo.c", "UTF-8");
        var lexemes = new[]
        {
            new Lexeme("k", "1", new Position(1, 1), LexemeContentFactory.String("hello")),
            new Lexeme("2v", "2", new Position(1, 7), LexemeContentFactory.Number(42)),
            new Lexeme("b", "3", new Position(2, 1), LexemeContentFactory.Boolean(true)),
            new Lexeme("0", "4", new Position(2, 5), LexemeContent.Empty)
        };

        // Test each format
        var formats = new[] { "text", "json", "csv", "xml" };

        foreach (var format in formats)
        {
            Console.WriteLine($"=== {format.ToUpperInvariant()} Format ===");
            
            try
            {
                var output = new StringBuilder();
                using var writer = new StringWriter(output);
                using var formatter = FormatterFactory.CreateFormatter(format, writer);

                formatter.WriteHeader(header);
                foreach (var lexeme in lexemes)
                {
                    formatter.WriteLexeme(lexeme);
                }
                formatter.WriteFooter(lexemes.Length);

                Console.WriteLine(output.ToString());
                Console.WriteLine("✅ Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("Demo completed!");
    }
}
