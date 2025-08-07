using LexemeExtractor.Models;
using LexemeExtractor.Parsing;

// Test the lexeme name definition parsing functionality
Console.WriteLine("Testing Lexeme Name Definition Parsing");
Console.WriteLine("=====================================");

// Test parsing individual lines
var testLines = new[]
{
    "large_unsigned_integer_number = :20b RATIONAL;",
    "exec_record_identifier = :248 STRING;",
    "'PREFIX' = :97;",
    "program_name = :1a2 IDENTIFIER;",
    "'WORKING-STORAGE' = :2c4;"
};

Console.WriteLine("Testing individual line parsing:");
foreach (var line in testLines)
{
    var definition = LexemeNameDefinitionParser.ParseLine(line);
    if (definition != null)
    {
        Console.WriteLine($"  {line}");
        Console.WriteLine($"    -> Name: '{definition.Name}', Number: {definition.Number} (0x{definition.Number:X}), Type: '{definition.DataType}'");
    }
    else
    {
        Console.WriteLine($"  {line} -> FAILED TO PARSE");
    }
}

Console.WriteLine();

// Test parsing the sample file
var sampleFilePath = "LexemeExtractor/SampleInput/COBOL~IBMEnterprise.txt";
if (File.Exists(sampleFilePath))
{
    Console.WriteLine($"Testing file parsing: {sampleFilePath}");
    var definitions = LexemeNameDefinitionParser.ParseFile(sampleFilePath);
    Console.WriteLine($"Parsed {definitions.Count} definitions:");
    
    foreach (var kvp in definitions)
    {
        var def = kvp.Value;
        Console.WriteLine($"  {def.Name} = :{def.Number:X} {def.DataType ?? "(no type)"}");
    }
    
    Console.WriteLine();
    
    // Test number lookup
    var numberLookup = LexemeNameDefinitionParser.CreateNumberLookup(definitions);
    Console.WriteLine("Testing number lookup:");
    var testNumbers = new long[] { 0x20b, 0x248, 0x97, 0x1a2 };
    foreach (var number in testNumbers)
    {
        if (numberLookup.TryGetValue(number, out var def))
        {
            Console.WriteLine($"  Number {number} (0x{number:X}) -> {def.Name}");
        }
        else
        {
            Console.WriteLine($"  Number {number} (0x{number:X}) -> NOT FOUND");
        }
    }
}
else
{
    Console.WriteLine($"Sample file not found: {sampleFilePath}");
}

Console.WriteLine();

// Test parsing a lexeme file with name definitions
var lexemeFilePath = "LexemeExtractor/SampleInput/IND2000.cbl.lexemes";
if (File.Exists(lexemeFilePath))
{
    Console.WriteLine($"Testing lexeme file parsing with name definitions: {lexemeFilePath}");
    try
    {
        var lexemeFile = LexemeFileParser.ParseFile(lexemeFilePath);
        Console.WriteLine($"Parsed {lexemeFile.Lexemes.Count} lexemes from {lexemeFile.Header.Domain}");
        
        // Show first few lexemes with their name definitions
        Console.WriteLine("First 10 lexemes:");
        foreach (var lexeme in lexemeFile.Lexemes.Take(10))
        {
            var nameDisplay = lexeme.NameDefinition?.Name ?? $"#{lexeme.Number}";
            Console.WriteLine($"  {nameDisplay} (Type: {lexeme.Type}, Number: {lexeme.Number}) at {lexeme.Position}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing lexeme file: {ex.Message}");
    }
}
else
{
    Console.WriteLine($"Lexeme file not found: {lexemeFilePath}");
}

Console.WriteLine("\nTest completed.");
