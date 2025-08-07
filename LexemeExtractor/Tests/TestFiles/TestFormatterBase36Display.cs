using System.Text;
using LexemeExtractor.Models;
using LexemeExtractor.OutputFormatters;

/// <summary>
/// Test program to verify Base36 number display in all formatters
/// </summary>
class TestFormatterBase36Display
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Testing Base36 Number Display in Formatters ===");
        Console.WriteLine();

        // Create test data with various Base36 numbers
        var header = new FileHeader("C~~1.0", "test.c", "UTF-8");
        var lexemes = new[]
        {
            new Lexeme("k", "1", new Position(1, 1), LexemeContentFactory.String("hello")),
            new Lexeme("2v", "a", new Position(1, 7), LexemeContentFactory.Number(42)),
            new Lexeme("b", "1z", new Position(2, 1), LexemeContentFactory.Boolean(true)),
            new Lexeme("0", "zz", new Position(2, 5), LexemeContent.Empty)
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

                var result = output.ToString();
                Console.WriteLine(result);

                // Verify format-specific requirements
                VerifyFormatOutput(format, result);
                
                Console.WriteLine("✅ Format verification passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error with {format}: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("🎉 Base36 display formatting tests completed!");
    }

    static void VerifyFormatOutput(string format, string output)
    {
        switch (format.ToLowerInvariant())
        {
            case "text":
                // Should show: #1(1), #a(10), #1z(71), #zz(1295)
                if (!output.Contains("#1(1)"))
                    throw new Exception("Text format missing #1(1)");
                if (!output.Contains("#a(10)"))
                    throw new Exception("Text format missing #a(10)");
                if (!output.Contains("#1z(71)"))
                    throw new Exception("Text format missing #1z(71)");
                if (!output.Contains("#zz(1295)"))
                    throw new Exception("Text format missing #zz(1295)");
                break;

            case "json":
                // Should have both NumberString and Number fields
                if (!output.Contains("\"NumberString\": \"1\""))
                    throw new Exception("JSON missing NumberString field for '1'");
                if (!output.Contains("\"Number\": 1"))
                    throw new Exception("JSON missing Number field for 1");
                if (!output.Contains("\"NumberString\": \"a\""))
                    throw new Exception("JSON missing NumberString field for 'a'");
                if (!output.Contains("\"Number\": 10"))
                    throw new Exception("JSON missing Number field for 10");
                if (!output.Contains("\"NumberString\": \"1z\""))
                    throw new Exception("JSON missing NumberString field for '1z'");
                if (!output.Contains("\"Number\": 71"))
                    throw new Exception("JSON missing Number field for 71");
                if (!output.Contains("\"NumberString\": \"zz\""))
                    throw new Exception("JSON missing NumberString field for 'zz'");
                if (!output.Contains("\"Number\": 1295"))
                    throw new Exception("JSON missing Number field for 1295");
                break;

            case "csv":
                // Should have separate columns for NumberString and Number
                if (!output.Contains("Type,NumberString,Number,"))
                    throw new Exception("CSV missing NumberString column header");
                if (!output.Contains("k,1,1,"))
                    throw new Exception("CSV missing k,1,1 row");
                if (!output.Contains("2v,a,10,"))
                    throw new Exception("CSV missing 2v,a,10 row");
                if (!output.Contains("b,1z,71,"))
                    throw new Exception("CSV missing b,1z,71 row");
                if (!output.Contains("0,zz,1295,"))
                    throw new Exception("CSV missing 0,zz,1295 row");
                break;

            case "xml":
                // Should have separate NumberString and Number elements
                if (!output.Contains("<NumberString>1</NumberString>"))
                    throw new Exception("XML missing NumberString element for '1'");
                if (!output.Contains("<Number>1</Number>"))
                    throw new Exception("XML missing Number element for 1");
                if (!output.Contains("<NumberString>a</NumberString>"))
                    throw new Exception("XML missing NumberString element for 'a'");
                if (!output.Contains("<Number>10</Number>"))
                    throw new Exception("XML missing Number element for 10");
                if (!output.Contains("<NumberString>1z</NumberString>"))
                    throw new Exception("XML missing NumberString element for '1z'");
                if (!output.Contains("<Number>71</Number>"))
                    throw new Exception("XML missing Number element for 71");
                if (!output.Contains("<NumberString>zz</NumberString>"))
                    throw new Exception("XML missing NumberString element for 'zz'");
                if (!output.Contains("<Number>1295</Number>"))
                    throw new Exception("XML missing Number element for 1295");
                break;
        }
    }
}
