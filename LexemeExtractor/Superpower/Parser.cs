using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static Superpower.Parse;

namespace LexemeExtractor.Superpower;

using System;
using System.Collections.Generic;
using System.Linq;

// AST Types with primary constructors
public record File(string Domain, string FileSourceInformation, string Encoding, List<Lexeme> Lexemes);
public record Lexeme(char Type, string Radix36Number, Position Position, Content Content);

public abstract record Position;
public record SamePosition(int Width) : Position;
public record SameLineEndColumn(Column EndColumn) : Position;
public record SameLineStartColumn(Column StartColumn, int Width) : Position;
public record SameLineRange(Column StartColumn, Column EndColumn) : Position;
public record NextLineRange(Column StartColumn, Column EndColumn) : Position;
public record FullRange(StartPosition Start, EndPosition End) : Position;

public abstract record StartPosition;
public abstract record EndPosition;
public record AbsoluteLineStartPosition(int LineNumber, Column Column) : StartPosition;
public record AbsoluteLineEndPosition(int LineNumber, Column Column) : EndPosition;
public record SameLineStartPosition(Column Column) : StartPosition;
public record SameLineEndPosition(Column Column) : EndPosition;
public record PunctuationLineStartPosition(char Punctuation, Column Column) : StartPosition;
public record PunctuationLineEndPosition(char Punctuation, Column Column) : EndPosition;
public record EncodedStartPosition(EncodedPositionType Type) : StartPosition;
public record EncodedEndPosition(EncodedPositionType Type) : EndPosition;

public enum EncodedPositionType { SameLineColumn, ColumnPlusOne, ColumnPlusTwo }

public abstract record Column;
public record AbsoluteColumn(int Number) : Column;
public record RelativeColumn(char Radix52Digit) : Column;
public record SameColumn : Column;

public abstract record Content;
public record EmptyContent : Content;
public record StringContent(string Value) : Content;
public record IntegerContent(int Value, char? Sign = null) : Content;
public record FloatContent(double Value, char? Sign = null) : Content;
public record BooleanContent(bool Value) : Content;

// Text Parsers
public static class CompressedLexemeParser
{
    // Basic parsers
    static readonly TextParser<char> TypeParser = Character.In("ABCDEFGHIJKLMNO".ToCharArray());
    
    static readonly TextParser<string> Radix36NumberParser =
        Character.LetterOrDigit.Or(Character.Lower)
            .AtLeastOnce()
            .Select(chars => new string(chars));

    static readonly TextParser<int> NumberParser = Numerics.IntegerInt32;

    static readonly TextParser<double> FloatParser =
        from sign in Character.In('+', '-').OptionalOrDefault()
        from whole in Span.Regex(@"\d+")
        from dot in Character.EqualTo('.')
        from fraction in Span.Regex(@"\d+")
        from exp in Span.Regex(@"[eE][+-]?\d+").Optional()
        select double.Parse($"{(sign == '-' ? "-" : "")}{whole}.{fraction}{(exp.HasValue ? exp.Value.ToStringValue() : "")}");

    // Column parsers
    static readonly TextParser<Column> ColumnParser =
        NumberParser.Select(n => (Column)new AbsoluteColumn(n))
        .Or(Character.EqualTo('=').Select(_ => (Column)new SameColumn()))
        .Or(Character.Letter.Select(c => (Column)new RelativeColumn(c)));

    // Position parsers
    static readonly TextParser<char> PunctuationParser = Character.In('!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/');

    static readonly TextParser<StartPosition> EncodedStartPositionParser =
        Character.In('@', '|', '_').Select(c => (StartPosition)new EncodedStartPosition(
            c switch
            {
                '@' => EncodedPositionType.SameLineColumn,
                '|' => EncodedPositionType.ColumnPlusOne,
                _ => EncodedPositionType.ColumnPlusTwo
            }));

    static readonly TextParser<EndPosition> EncodedEndPositionParser =
        Character.In('@', '|', '_').Select(c => (EndPosition)new EncodedEndPosition(
            c switch
            {
                '@' => EncodedPositionType.SameLineColumn,
                '|' => EncodedPositionType.ColumnPlusOne,
                _ => EncodedPositionType.ColumnPlusTwo
            }));

    static readonly TextParser<StartPosition> StartPositionParser =
        EncodedStartPositionParser
        .Or(from line in NumberParser
            from _ in Character.WhiteSpace
            from col in ColumnParser
            select (StartPosition)new AbsoluteLineStartPosition(line, col))
        .Or(from _ in Character.EqualTo('=')
            from __ in Character.WhiteSpace
            from col in ColumnParser
            select (StartPosition)new SameLineStartPosition(col))
        .Or(from punct in PunctuationParser
            from _ in Character.WhiteSpace
            from col in ColumnParser
            select (StartPosition)new PunctuationLineStartPosition(punct, col));

    static readonly TextParser<EndPosition> EndPositionParser =
        EncodedEndPositionParser
        .Or(from line in NumberParser
            from _ in Character.WhiteSpace
            from col in ColumnParser
            select (EndPosition)new AbsoluteLineEndPosition(line, col))
        .Or(from _ in Character.EqualTo('=')
            from __ in Character.WhiteSpace
            from col in ColumnParser
            select (EndPosition)new SameLineEndPosition(col))
        .Or(from punct in PunctuationParser
            from _ in Character.WhiteSpace
            from col in ColumnParser
            select (EndPosition)new PunctuationLineEndPosition(punct, col));

    // Position parser - handle various position encoding patterns
    static readonly TextParser<Position> PositionParser =
        // Single character positions
        Character.In(':', ';', '@', '|', '_').Select(_ => (Position)new SamePosition(1))
        // Bracket-based positions
        .Or(from bracket in Character.In('[', ']')
            from chars in Character.Letter.AtLeastOnce()
            select (Position)new SamePosition(1))
        // Punctuation-based line increment (quote followed by column, but not string content)
        .Or(from punct in Character.In('!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/')
            from col in Character.Letter
            select (Position)new SamePosition(1))
        // Other position patterns
        .Or(Character.ExceptIn('\n', '\r', '+', '-', '~').Where(c => !char.IsDigit(c) || c == '0').AtLeastOnce()
            .Select(_ => (Position)new SamePosition(1)))
        // Empty position
        .Or(Return<Position>(new SamePosition(1)));

    // String content parser - quote followed by content (no closing quote required)
    static readonly TextParser<string> StringContentParser =
        from _ in Character.EqualTo('"')
        from content in Character.ExceptIn('\n', '\r').Many()
        select new string(content);

    static readonly TextParser<Content> ContentParser =
        // String content (starts with quote)
        StringContentParser.Select(s => (Content)new StringContent(s))
        // Signed numbers
        .Or(from sign in Character.In('+', '-')
            from num in NumberParser
            select (Content)new IntegerContent(sign == '-' ? -num : num, sign))
        // Unsigned numbers
        .Or(NumberParser.Select(num => (Content)new IntegerContent(num)))
        // Float numbers
        .Or(FloatParser.Select(f => (Content)new FloatContent(f)))
        // Boolean values
        .Or(Span.EqualTo("~t").Select(_ => (Content)new BooleanContent(true)))
        .Or(Span.EqualTo("~f").Select(_ => (Content)new BooleanContent(false)))
        // Everything else as string content or empty
        .Or(Character.ExceptIn('\n', '\r').Many().Select(chars =>
            chars.Length == 0 ? (Content)new EmptyContent() : (Content)new StringContent(new string(chars))));

    // Lexeme parser
    static readonly TextParser<Lexeme> LexemeParser =
        from type in TypeParser.OptionalOrDefault()
        from radix36 in Radix36NumberParser
        from position in PositionParser
        from content in ContentParser
        from _ in Character.In('\n', '\r').AtLeastOnce()
        select new Lexeme(type == '\0' ? '\0' : type, radix36, position, content);

    // File parser
    static readonly TextParser<File> FileParser =
        from domain in Character.ExceptIn('\n', '\r').Many().Select(chars => new string(chars))
        from _1 in Character.In('\n', '\r').AtLeastOnce()
        from fileSourceInfo in Character.ExceptIn('\n', '\r').Many().Select(chars => new string(chars))
        from _2 in Character.In('\n', '\r').AtLeastOnce()
        from encoding in Character.ExceptIn('\n', '\r').Many().Select(chars => new string(chars))
        from _3 in Character.In('\n', '\r').AtLeastOnce()
        from lexemes in LexemeParser.Many()
        select new File(domain, fileSourceInfo, encoding, lexemes.ToList());

    // Main parse method
    public static File Parse(string input) => FileParser.Parse(input);
}