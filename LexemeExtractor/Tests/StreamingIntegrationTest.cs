using System.Text;
using LexemeExtractor.Models;
using LexemeExtractor.OutputFormatters;
using LexemeExtractor.Parsing;

namespace LexemeExtractor.Tests;

/// <summary>
/// Integration tests for streaming functionality
/// </summary>
public static class StreamingIntegrationTest
{
    /// <summary>
    /// Sample lexeme file content for testing
    /// </summary>
    private const string SampleLexemeContent = """
        C~~1.0
        test.c
        UTF-8
        k1A"hello"
        2v2B+42
        b3C~t
        04D
        """;

    /// <summary>
    /// Tests streaming parsing and formatting end-to-end
    /// </summary>
    public static void TestEndToEndStreaming()
    {
        Console.WriteLine("=== Testing End-to-End Streaming ===");
        Console.WriteLine();

        var formats = new[] { "text", "json", "csv", "xml" };

        foreach (var format in formats)
        {
            Console.WriteLine($"Testing {format.ToUpperInvariant()} format:");
            Console.WriteLine(new string('-', 40));

            try
            {
                var output = new StringBuilder();
                using var writer = new StringWriter(output);
                using var formatter = FormatterFactory.CreateFormatter(format, writer);

                // Simulate streaming parsing
                using var reader = new StringReader(SampleLexemeContent);
                ProcessStreamWithFormatter(reader, formatter);

                var result = output.ToString();
                Console.WriteLine(result);
                Console.WriteLine();

                // Basic validation
                ValidateOutput(format, result);
                Console.WriteLine($"✅ {format.ToUpperInvariant()} format test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {format.ToUpperInvariant()} format test failed: {ex.Message}");
                throw;
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Processes a stream with a formatter (similar to Program.cs logic)
    /// </summary>
    private static void ProcessStreamWithFormatter(TextReader reader, ILexemeFormatter formatter)
    {
        // Parse header
        var domain = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing domain line");
        var filename = reader.ReadLine()?.Trim() ?? throw new FormatException("Missing filename line");
        var encoding = reader.ReadLine()?.Trim() ?? "UTF-8";
        var header = new FileHeader(domain, filename, encoding);
        
        formatter.WriteHeader(header);

        // Stream lexemes
        var positionDecoder = new PositionDecoder();
        var count = 0;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var lexeme = LexemeFileParser.ParseLexemeLine(line, positionDecoder);
            formatter.WriteLexeme(lexeme);
            count++;
        }

        formatter.WriteFooter(count);
    }

    /// <summary>
    /// Validates output for each format
    /// </summary>
    private static void ValidateOutput(string format, string output)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                if (!output.Contains("\"Domain\": \"C~~1.0\""))
                    throw new Exception("JSON missing domain");
                if (!output.Contains("\"LexemeCount\": 4"))
                    throw new Exception("JSON missing count");
                break;

            case "csv":
                if (!output.Contains("Type,Number,Line,Column"))
                    throw new Exception("CSV missing header");
                if (!output.Contains("k,1,1,1"))
                    throw new Exception("CSV missing first lexeme");
                break;

            case "text":
                if (!output.Contains("Domain: C~~1.0"))
                    throw new Exception("Text missing domain");
                if (!output.Contains("Total lexemes processed: 4"))
                    throw new Exception("Text missing count");
                break;

            case "xml":
                if (!output.Contains("<Domain>C~~1.0</Domain>"))
                    throw new Exception("XML missing domain");
                if (!output.Contains("<LexemeCount>4</LexemeCount>"))
                    throw new Exception("XML missing count");
                break;
        }
    }

    /// <summary>
    /// Runs all streaming tests
    /// </summary>
    public static void RunAllTests()
    {
        try
        {
            StreamingFormatterTests.RunAllTests();
            TestEndToEndStreaming();
            Console.WriteLine("🎉 All streaming tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Streaming tests failed: {ex.Message}");
            throw;
        }
    }
}
