using LexemeExtractor.Models;
using LexemeExtractor.Parsing;

/// <summary>
/// Test program to verify parsing works with Base36 string storage
/// </summary>
class TestParsingWithBase36
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Testing Parsing with Base36 String Storage ===");
        Console.WriteLine();

        // Sample lexeme content for testing
        var sampleContent = """
            C~~1.0
            test.c
            UTF-8
            k1A"hello"
            2va+42
            b1z~t
            0zz
            """;

        try
        {
            Console.WriteLine("Parsing sample lexeme content:");
            Console.WriteLine(new string('-', 40));

            var lexemeFile = LexemeFileParser.ParseText(sampleContent, "test");
            
            Console.WriteLine($"Header: {lexemeFile.Header}");
            Console.WriteLine($"Total lexemes: {lexemeFile.Count}");
            Console.WriteLine();

            foreach (var lexeme in lexemeFile)
            {
                Console.WriteLine($"Type: '{lexeme.Type}'");
                Console.WriteLine($"  NumberString: '{lexeme.NumberString}'");
                Console.WriteLine($"  Number: {lexeme.Number}");
                Console.WriteLine($"  Position: {lexeme.Position}");
                Console.WriteLine($"  Content: {lexeme.Content}");
                Console.WriteLine($"  ToString: {lexeme}");
                Console.WriteLine();
            }

            // Verify specific cases
            var lexemes = lexemeFile.ToArray();
            
            // First lexeme: k1A"hello"
            if (lexemes.Length > 0)
            {
                var first = lexemes[0];
                if (first.NumberString == "1" && first.Number == 1)
                    Console.WriteLine("✅ First lexeme Base36 conversion correct");
                else
                    Console.WriteLine($"❌ First lexeme failed: NumberString='{first.NumberString}', Number={first.Number}");
            }

            // Second lexeme: 2va+42 (Base36 'a' = 10)
            if (lexemes.Length > 1)
            {
                var second = lexemes[1];
                if (second.NumberString == "a" && second.Number == 10)
                    Console.WriteLine("✅ Second lexeme Base36 conversion correct");
                else
                    Console.WriteLine($"❌ Second lexeme failed: NumberString='{second.NumberString}', Number={second.Number}");
            }

            // Third lexeme: b1z~t (Base36 '1z' = 1*36 + 35 = 71)
            if (lexemes.Length > 2)
            {
                var third = lexemes[2];
                if (third.NumberString == "1z" && third.Number == 71)
                    Console.WriteLine("✅ Third lexeme Base36 conversion correct");
                else
                    Console.WriteLine($"❌ Third lexeme failed: NumberString='{third.NumberString}', Number={third.Number}");
            }

            // Fourth lexeme: 0zz (Base36 'zz' = 35*36 + 35 = 1295)
            if (lexemes.Length > 3)
            {
                var fourth = lexemes[3];
                if (fourth.NumberString == "zz" && fourth.Number == 1295)
                    Console.WriteLine("✅ Fourth lexeme Base36 conversion correct");
                else
                    Console.WriteLine($"❌ Fourth lexeme failed: NumberString='{fourth.NumberString}', Number={fourth.Number}");
            }

            Console.WriteLine();
            Console.WriteLine("🎉 Parsing with Base36 string storage test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Parsing test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
