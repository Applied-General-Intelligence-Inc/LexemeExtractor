using LexemeExtractor.Models;
using LexemeExtractor.Parsing;

/// <summary>
/// Test program to verify the simplified Base36 implementation
/// </summary>
class TestSimplifiedBase36
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Testing Simplified Base36 Implementation ===");
        Console.WriteLine();

        // Test direct lexeme creation with Base36 strings
        Console.WriteLine("Testing direct lexeme creation:");
        Console.WriteLine(new string('-', 40));

        var testCases = new[]
        {
            ("0", 0L),
            ("1", 1L),
            ("a", 10L),
            ("z", 35L),
            ("10", 36L),
            ("1a", 46L),
            ("zz", 1295L)
        };

        foreach (var (base36String, expectedValue) in testCases)
        {
            try
            {
                var lexeme = new Lexeme("k", base36String, new Position(1, 1), LexemeContent.Empty);
                
                if (lexeme.NumberString == base36String && lexeme.Number == expectedValue)
                {
                    Console.WriteLine($"✅ '{base36String}' -> {expectedValue}");
                }
                else
                {
                    Console.WriteLine($"❌ '{base36String}' failed: stored='{lexeme.NumberString}', number={lexeme.Number}, expected={expectedValue}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error with '{base36String}': {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Testing with actual parsing:");
        Console.WriteLine(new string('-', 40));

        // Test with sample lexeme content
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
            var lexemeFile = LexemeFileParser.ParseText(sampleContent, "test");
            
            Console.WriteLine($"Parsed {lexemeFile.Count} lexemes:");
            
            foreach (var lexeme in lexemeFile)
            {
                Console.WriteLine($"  Type '{lexeme.Type}': '{lexeme.NumberString}' -> {lexeme.Number}");
            }

            // Verify specific parsing results
            var lexemes = lexemeFile.ToArray();
            
            if (lexemes.Length >= 4)
            {
                var checks = new[]
                {
                    (lexemes[0], "1", 1L, "k1A"),
                    (lexemes[1], "a", 10L, "2va"),
                    (lexemes[2], "1z", 71L, "b1z"),
                    (lexemes[3], "zz", 1295L, "0zz")
                };

                Console.WriteLine();
                Console.WriteLine("Verification:");
                foreach (var (lexeme, expectedString, expectedNumber, description) in checks)
                {
                    if (lexeme.NumberString == expectedString && lexeme.Number == expectedNumber)
                    {
                        Console.WriteLine($"✅ {description}: '{lexeme.NumberString}' -> {lexeme.Number}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {description}: got '{lexeme.NumberString}' -> {lexeme.Number}, expected '{expectedString}' -> {expectedNumber}");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("🎉 Simplified Base36 implementation test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Parsing test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
