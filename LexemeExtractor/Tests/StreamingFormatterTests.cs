using System.Text;
using LexemeExtractor.Models;
using LexemeExtractor.OutputFormatters;

namespace LexemeExtractor.Tests;

/// <summary>
/// Tests for streaming formatters to verify they work correctly
/// </summary>
public class StreamingFormatterTests
{
    /// <summary>
    /// Test data for formatter testing
    /// </summary>
    private static readonly FileHeader TestHeader = new("TestDomain~~1.0", "test.lexemes", "UTF-8");
    
    private static readonly Lexeme[] TestLexemes =
    [
        new("k", "1", new Position(1, 1), LexemeContentFactory.String("hello")),
        new("2v", "2", new Position(1, 7), LexemeContentFactory.Number(42)),
        new("b", "3", new Position(2, 1), LexemeContentFactory.Boolean(true)),
        new("0", "4", new Position(2, 5), LexemeContent.Empty)
    ];

    /// <summary>
    /// Tests JSON streaming formatter
    /// </summary>
    public static void TestJsonFormatter()
    {
        var output = new StringBuilder();
        using var writer = new StringWriter(output);
        using var formatter = new JsonStreamingFormatter(writer);

        formatter.WriteHeader(TestHeader);
        foreach (var lexeme in TestLexemes)
        {
            formatter.WriteLexeme(lexeme);
        }
        formatter.WriteFooter(TestLexemes.Length);

        var result = output.ToString();
        Console.WriteLine("JSON Output:");
        Console.WriteLine(result);
        Console.WriteLine();

        // Basic validation
        if (!result.Contains("\"Domain\": \"TestDomain~~1.0\""))
            throw new Exception("JSON output missing domain");
        if (!result.Contains("\"LexemeCount\": 4"))
            throw new Exception("JSON output missing lexeme count");
    }

    /// <summary>
    /// Tests CSV streaming formatter
    /// </summary>
    public static void TestCsvFormatter()
    {
        var output = new StringBuilder();
        using var writer = new StringWriter(output);
        using var formatter = new CsvStreamingFormatter(writer);

        formatter.WriteHeader(TestHeader);
        foreach (var lexeme in TestLexemes)
        {
            formatter.WriteLexeme(lexeme);
        }
        formatter.WriteFooter(TestLexemes.Length);

        var result = output.ToString();
        Console.WriteLine("CSV Output:");
        Console.WriteLine(result);
        Console.WriteLine();

        // Basic validation
        if (!result.Contains("Type,Number,Line,Column"))
            throw new Exception("CSV output missing header");
        if (!result.Contains("k,1,1,1"))
            throw new Exception("CSV output missing first lexeme");
    }

    /// <summary>
    /// Tests text streaming formatter
    /// </summary>
    public static void TestTextFormatter()
    {
        var output = new StringBuilder();
        using var writer = new StringWriter(output);
        using var formatter = new TextStreamingFormatter(writer);

        formatter.WriteHeader(TestHeader);
        foreach (var lexeme in TestLexemes)
        {
            formatter.WriteLexeme(lexeme);
        }
        formatter.WriteFooter(TestLexemes.Length);

        var result = output.ToString();
        Console.WriteLine("Text Output:");
        Console.WriteLine(result);
        Console.WriteLine();

        // Basic validation
        if (!result.Contains("Domain: TestDomain~~1.0"))
            throw new Exception("Text output missing domain");
        if (!result.Contains("Total lexemes processed: 4"))
            throw new Exception("Text output missing total count");
    }

    /// <summary>
    /// Tests XML streaming formatter
    /// </summary>
    public static void TestXmlFormatter()
    {
        var output = new StringBuilder();
        using var writer = new StringWriter(output);
        using var formatter = new XmlStreamingFormatter(writer);

        formatter.WriteHeader(TestHeader);
        foreach (var lexeme in TestLexemes)
        {
            formatter.WriteLexeme(lexeme);
        }
        formatter.WriteFooter(TestLexemes.Length);

        var result = output.ToString();
        Console.WriteLine("XML Output:");
        Console.WriteLine(result);
        Console.WriteLine();

        // Basic validation
        if (!result.Contains("<Domain>TestDomain~~1.0</Domain>"))
            throw new Exception("XML output missing domain");
        if (!result.Contains("<LexemeCount>4</LexemeCount>"))
            throw new Exception("XML output missing lexeme count");
    }

    /// <summary>
    /// Runs all formatter tests
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("=== Testing Streaming Formatters ===");
        Console.WriteLine();

        try
        {
            TestJsonFormatter();
            TestCsvFormatter();
            TestTextFormatter();
            TestXmlFormatter();

            Console.WriteLine("All streaming formatter tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
            throw;
        }
    }
}
