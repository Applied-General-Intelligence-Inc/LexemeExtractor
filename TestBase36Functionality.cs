using LexemeExtractor.Models;

/// <summary>
/// Test program to verify Base36 functionality in Lexeme
/// </summary>
class TestBase36Functionality
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Testing Base36 Functionality ===");
        Console.WriteLine();

        // Test cases for Base36 conversion
        var testCases = new[]
        {
            ("0", 0L),
            ("1", 1L),
            ("9", 9L),
            ("a", 10L),
            ("z", 35L),
            ("10", 36L),
            ("1a", 46L),
            ("zz", 1295L),
            ("100", 1296L)
        };

        Console.WriteLine("Testing Base36 string storage and Number getter:");
        Console.WriteLine(new string('-', 50));

        foreach (var (base36String, expectedValue) in testCases)
        {
            try
            {
                // Create lexeme with Base36 string
                var lexeme = new Lexeme("k", base36String, new Position(1, 1), LexemeContent.Empty);
                
                // Verify the string is stored correctly
                if (lexeme.NumberString != base36String)
                {
                    Console.WriteLine($"❌ String storage failed: expected '{base36String}', got '{lexeme.NumberString}'");
                    continue;
                }
                
                // Verify the Number getter converts correctly
                if (lexeme.Number != expectedValue)
                {
                    Console.WriteLine($"❌ Conversion failed: '{base36String}' -> expected {expectedValue}, got {lexeme.Number}");
                    continue;
                }
                
                Console.WriteLine($"✅ '{base36String}' -> {expectedValue} (stored as '{lexeme.NumberString}')");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error with '{base36String}': {ex.Message}");
            }
        }

        Console.WriteLine();
                    continue;
                }
                
                Console.WriteLine($"✅ {value} -> '{lexeme.NumberString}' -> {lexeme.Number}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error with {value}: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Testing with actual lexeme parsing:");
        Console.WriteLine(new string('-', 50));

        // Test with sample lexeme data that would come from parsing
        var sampleLexemes = new[]
        {
            ("k", "1"),      // Simple case
            ("2v", "a"),     // Base36 'a' = 10
            ("b", "1z"),     // Base36 '1z' = 71
            ("0", "zz")      // Base36 'zz' = 1295
        };

        foreach (var (type, numberString) in sampleLexemes)
        {
            try
            {
                var lexeme = new Lexeme(type, numberString, new Position(1, 1), LexemeContent.Empty);
                Console.WriteLine($"✅ Type '{type}', NumberString '{lexeme.NumberString}', Number {lexeme.Number}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error with type '{type}', number '{numberString}': {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("🎉 Base36 functionality tests completed!");
    }
}
