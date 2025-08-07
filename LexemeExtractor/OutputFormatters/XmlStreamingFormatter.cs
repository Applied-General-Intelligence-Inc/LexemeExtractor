using LexemeExtractor.Models;
using System.Xml;
using StringContent = LexemeExtractor.Models.StringContent;

namespace LexemeExtractor.OutputFormatters;

/// <summary>
/// Streaming XML formatter that outputs lexemes as they are parsed
/// </summary>
public class XmlStreamingFormatter : StreamingFormatterBase
{
    /// <summary>
    /// Creates a new XML streaming formatter
    /// </summary>
    /// <param name="writer">TextWriter to output XML to</param>
    public XmlStreamingFormatter(TextWriter writer) : base(writer)
    {
    }

    /// <summary>
    /// Writes the XML header with file information
    /// </summary>
    public override void WriteHeader(FileHeader header)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(XmlStreamingFormatter));

        Writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        Writer.WriteLine("<LexemeFile>");
        Writer.WriteLine("  <Header>");
        Writer.WriteLine($"    <Domain>{EscapeXml(header.Domain)}</Domain>");
        Writer.WriteLine($"    <Filename>{EscapeXml(header.Filename)}</Filename>");
        Writer.WriteLine($"    <Encoding>{EscapeXml(header.Encoding)}</Encoding>");
        Writer.WriteLine("  </Header>");
        Writer.WriteLine("  <Lexemes>");
    }

    /// <summary>
    /// Writes a single lexeme in XML format
    /// </summary>
    protected override void WriteFormattedLexeme(Lexeme lexeme)
    {
        Writer.WriteLine("    <Lexeme>");
        Writer.WriteLine($"      <Type>{EscapeXml(lexeme.Type)}</Type>");
        Writer.WriteLine($"      <NumberString>{EscapeXml(lexeme.NumberString)}</NumberString>");
        Writer.WriteLine($"      <Number>{lexeme.Number}</Number>");
        WritePosition(lexeme.Position);
        WriteContent(lexeme.Content);
        Writer.WriteLine("    </Lexeme>");
    }

    /// <summary>
    /// Writes the XML footer with total count
    /// </summary>
    public override void WriteFooter(int totalCount)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(XmlStreamingFormatter));

        Writer.WriteLine("  </Lexemes>");
        Writer.WriteLine($"  <LexemeCount>{totalCount}</LexemeCount>");
        Writer.WriteLine("</LexemeFile>");
    }

    /// <summary>
    /// Writes position information in XML format
    /// </summary>
    private void WritePosition(Position position)
    {
        Writer.WriteLine("      <Position>");
        Writer.WriteLine($"        <Line>{position.Line}</Line>");
        Writer.WriteLine($"        <Column>{position.Column}</Column>");
        
        if (position.Length.HasValue)
            Writer.WriteLine($"        <Length>{position.Length}</Length>");
        
        if (position.EndLine.HasValue)
            Writer.WriteLine($"        <EndLine>{position.EndLine}</EndLine>");
        
        if (position.EndColumn.HasValue)
            Writer.WriteLine($"        <EndColumn>{position.EndColumn}</EndColumn>");
        
        Writer.WriteLine($"        <IsRange>{position.IsRange.ToString().ToLowerInvariant()}</IsRange>");
        Writer.WriteLine("      </Position>");
    }

    /// <summary>
    /// Writes content information in XML format
    /// </summary>
    private void WriteContent(LexemeContent content)
    {
        Writer.WriteLine("      <Content>");
        
        switch (content)
        {
            case StringContent sc:
                Writer.WriteLine($"        <Type>string</Type>");
                Writer.WriteLine($"        <Value>{EscapeXml(sc.StringValue)}</Value>");
                break;
            case NumberContent nc:
                Writer.WriteLine($"        <Type>number</Type>");
                Writer.WriteLine($"        <Value>{nc.NumberValue}</Value>");
                break;
            case BooleanContent bc:
                Writer.WriteLine($"        <Type>boolean</Type>");
                Writer.WriteLine($"        <Value>{bc.BooleanValue.ToString().ToLowerInvariant()}</Value>");
                break;
            case EmptyContent:
                Writer.WriteLine($"        <Type>empty</Type>");
                break;
            default:
                Writer.WriteLine($"        <Type>unknown</Type>");
                Writer.WriteLine($"        <Value>{EscapeXml(content.ToString())}</Value>");
                break;
        }
        
        Writer.WriteLine("      </Content>");
    }

    /// <summary>
    /// Escapes a string for XML output
    /// </summary>
    private static string EscapeXml(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
