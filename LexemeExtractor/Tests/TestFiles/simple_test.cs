// Simple test for lexeme name definition parsing
using System.Text.RegularExpressions;

Console.WriteLine("Testing Lexeme Name Definition Parsing");
Console.WriteLine("=====================================");

// Test parsing individual lines with a simple regex
var testLines = new[]
{
    "large_unsigned_integer_number = :20b RATIONAL;",
    "exec_record_identifier = :248 STRING;",
    "'PREFIX' = :97;",
    "program_name = :1a2 IDENTIFIER;",
    "'WORKING-STORAGE' = :2c4;"
};

// Simple regex pattern to match: optional_quotes_name = :hex_number optional_type;
var pattern = new Regex(@"^(?:'([^']+)'|([^=\s]+))\s*=\s*:([0-9A-Fa-f]+)(?:\s+([^;]+))?\s*;?\s*$");

Console.WriteLine("Testing individual line parsing:");
foreach (var line in testLines)
{
    var match = pattern.Match(line);
    if (match.Success)
    {
        // Extract name (either quoted or unquoted)
        var name = !string.IsNullOrEmpty(match.Groups[1].Value) 
            ? match.Groups[1].Value  // Quoted name
            : match.Groups[2].Value; // Unquoted name

        // Extract hex number and convert to decimal
        var hexNumber = match.Groups[3].Value;
        var number = Convert.ToInt64(hexNumber, 16);

        // Extract optional data type
        var dataType = match.Groups[4].Success && !string.IsNullOrWhiteSpace(match.Groups[4].Value)
            ? match.Groups[4].Value.Trim()
            : null;

        Console.WriteLine($"  {line}");
        Console.WriteLine($"    -> Name: '{name}', Number: {number} (0x{number:X}), Type: '{dataType ?? "(no type)"}'");
    }
    else
    {
        Console.WriteLine($"  {line} -> FAILED TO PARSE");
    }
}

Console.WriteLine("\nTest completed.");
