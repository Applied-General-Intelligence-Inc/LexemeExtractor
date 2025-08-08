using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static Superpower.Parse;
using LexemeExtractor.Models;

namespace LexemeExtractor.Superpower;

using System;
using System.Collections.Generic;
using System.Linq;



// Absolute position record for method parameters and returns
public record AbsolutePosition(int StartLine, int StartColumn, int EndLine, int EndColumn)
{
    public int StartLine = StartLine;
    public int StartColumn = StartColumn;
    public int EndLine = EndLine;
    public int EndColumn = EndColumn;
}

// AST Types
public record File(string Domain, string FileSourceInformation, string Encoding, List<Lexeme> Lexemes);
public record Lexeme(char Type, string Radix36Number, Position Position, Content Content);

public abstract record Position
{
    public abstract AbsolutePosition GetLexemePosition(AbsolutePosition currentPosition);
}

public record SamePosition(int Width) : Position
{
    public override AbsolutePosition GetLexemePosition(AbsolutePosition currentPosition) =>
        new AbsolutePosition(currentPosition.StartLine, currentPosition.EndColumn + 1, currentPosition.EndLine, currentPosition.EndColumn + Width);
}

public record SameLineEndColumn(Column EndColumn) : Position
{
    public override AbsolutePosition GetLexemePosition(AbsolutePosition currentPosition) =>
        new AbsolutePosition(currentPosition.StartLine, currentPosition.StartColumn, currentPosition.StartLine, EndColumn.GetAbsoluteColumn(currentPosition));
}

public record SameLineStartColumn(Column StartColumn, int Width) : Position
{
    public override AbsolutePosition GetLexemePosition(AbsolutePosition currentPosition)
    {
        var startCol = StartColumn.GetAbsoluteColumn(currentPosition);
        return new AbsolutePosition(currentPosition.StartLine, startCol, currentPosition.StartLine, startCol + Width - 1);
    }
}

public record SameLineRange(Column StartColumn, Column EndColumn) : Position
{
    public override AbsolutePosition GetLexemePosition(AbsolutePosition currentPosition)
    {
        var nextPosition = new AbsolutePosition(currentPosition.StartLine, StartColumn.GetAbsoluteColumn(currentPosition), currentPosition.StartLine, currentPosition.EndColumn);
        nextPosition.EndColumn = EndColumn.GetAbsoluteColumn(nextPosition);
        return nextPosition;
    }
}

public record NextLineRange(Column StartColumn, Column EndColumn) : Position
{
    public override AbsolutePosition GetLexemePosition(AbsolutePosition currentPosition)
    {
        // When moving to next line, create a position context with column reset to initial value
        var nextLine = currentPosition.StartLine + 1;
        var nextLinePosition = new AbsolutePosition(nextLine, Models.PositionConstants.InitialStartColumn, nextLine, Models.PositionConstants.InitialEndColumn);
        nextLinePosition.StartColumn = StartColumn.GetAbsoluteColumn(nextLinePosition);
        nextLinePosition.EndColumn = EndColumn.GetAbsoluteColumn(nextLinePosition);
        return nextLinePosition;
    }
}

public record FullRange(StartPosition Start, EndPosition End) : Position
{
    public override AbsolutePosition GetLexemePosition(AbsolutePosition currentPosition)
    {
        var newPosition = new AbsolutePosition(currentPosition.StartLine, currentPosition.StartColumn, currentPosition.EndLine, currentPosition.EndColumn);
        (newPosition.StartLine, newPosition.StartColumn) = Start.Position.GetAbsolutePosition(newPosition);
        if (newPosition.StartLine != currentPosition.StartLine)
        {
            newPosition.EndColumn = Models.PositionConstants.InitialEndColumn;
        }
        
        (newPosition.EndLine, newPosition.EndColumn) = End.Position.GetAbsolutePosition(newPosition);
        return newPosition;
    }
}

// Base position type that can be used as either start or end
public abstract record PositionBase
{
    public abstract (int LineNumber, int Column) GetAbsolutePosition(AbsolutePosition currentPosition);
}

public record AbsoluteLinePosition(int LineNumber, Column Column) : PositionBase
{
    public override (int LineNumber, int Column) GetAbsolutePosition(AbsolutePosition currentPosition)
    {
        // When line changes, create a position context with column reset to initial value
        var positionContext = LineNumber != currentPosition.StartLine
            ? new AbsolutePosition(LineNumber, Models.PositionConstants.InitialStartColumn, LineNumber, Models.PositionConstants.InitialEndColumn)
            : currentPosition;
        return (LineNumber, Column.GetAbsoluteColumn(positionContext));
    }
}

public record SameLinePosition(Column Column) : PositionBase
{
    public override (int LineNumber, int Column) GetAbsolutePosition(AbsolutePosition currentPosition) =>
        (currentPosition.StartLine, Column.GetAbsoluteColumn(currentPosition));
}

public record PunctuationLinePosition(char Punctuation, Column Column) : PositionBase
{
    public override (int LineNumber, int Column) GetAbsolutePosition(AbsolutePosition currentPosition)
    {
        // Punctuation characters !"... (codes #21-#2F) indicate "the last line number incremented by 1-15"
        // corresponding to the punctuation character code minus #20
        var increment = (int)Punctuation - 0x20;
        var lineNumber = currentPosition.StartLine + increment;

        // When line changes, create a position context with column reset to initial value
        var newLinePosition = new AbsolutePosition(lineNumber, Models.PositionConstants.InitialStartColumn, lineNumber, Models.PositionConstants.InitialEndColumn);
        return (lineNumber, Column.GetAbsoluteColumn(newLinePosition));
    }
}

public record EncodedPosition(EncodedPositionType Type) : PositionBase
{
    public override (int LineNumber, int Column) GetAbsolutePosition(AbsolutePosition currentPosition) => Type switch
    {
        EncodedPositionType.SameLineColumn => (LineNumber: currentPosition.StartLine, currentPosition.StartColumn),
        EncodedPositionType.ColumnPlusOne => (LineNumber: currentPosition.StartLine, currentPosition.StartColumn + 1),
        EncodedPositionType.ColumnPlusTwo => (LineNumber: currentPosition.StartLine, currentPosition.StartColumn + 2),
        _ => (LineNumber: currentPosition.StartLine, currentPosition.StartColumn)
    };
}

// Wrapper records for start and end positions
public record StartPosition(PositionBase Position);
public record EndPosition(PositionBase Position);

public enum EncodedPositionType { SameLineColumn, ColumnPlusOne, ColumnPlusTwo }

public abstract record Column
{
    public abstract int GetAbsoluteColumn(AbsolutePosition currentPosition);
}

public record AbsoluteColumn(int Number) : Column
{
    public override int GetAbsoluteColumn(AbsolutePosition currentPosition) => Number;
}

public record RelativeColumn(char Radix52Digit) : Column
{
    public override int GetAbsoluteColumn(AbsolutePosition currentPosition)
    {
        // Convert radix52 digit to offset
        var offset = Radix52Digit switch
        {
            >= 'a' and <= 'z' => Radix52Digit - 'a' + 27,
            >= 'A' and <= 'Z' => Radix52Digit - 'A' + 1,
            _ => 0
        };
        
        return Math.Max(currentPosition.StartColumn, currentPosition.EndColumn) + offset;
    }
}

public record SameColumn : Column
{
    public override int GetAbsoluteColumn(AbsolutePosition currentPosition) => currentPosition.StartColumn;
}

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
    static readonly TextParser<char> PunctuationParser = 
        Character.Matching(c => c >= '!' && c <= '/', "punctuation character (! through /)");

    static readonly TextParser<StartPosition> EncodedStartPositionParser =
        Character.In('@', '|', '_').Select(c => new StartPosition(new EncodedPosition(
            c switch
            {
                '@' => EncodedPositionType.SameLineColumn,
                '|' => EncodedPositionType.ColumnPlusOne,
                _ => EncodedPositionType.ColumnPlusTwo
            })));

    static readonly TextParser<EndPosition> EncodedEndPositionParser =
        Character.In('@', '|', '_').Select(c => new EndPosition(new EncodedPosition(
            c switch
            {
                '@' => EncodedPositionType.SameLineColumn,
                '|' => EncodedPositionType.ColumnPlusOne,
                _ => EncodedPositionType.ColumnPlusTwo
            })));

    static readonly TextParser<StartPosition> StartPositionParser =
        EncodedStartPositionParser
            .Or(from line in NumberParser
                from col in ColumnParser
                select new StartPosition(new AbsoluteLinePosition(line, col)))
            .Or(from _ in Character.EqualTo('=')
                from col in ColumnParser
                select new StartPosition(new SameLinePosition(col)))
            .Or(from punct in PunctuationParser
                from col in ColumnParser
                select new StartPosition(new PunctuationLinePosition(punct, col)));

    static readonly TextParser<EndPosition> EndPositionParser =
        EncodedEndPositionParser
            .Or(from line in NumberParser
                from col in ColumnParser
                select new EndPosition(new AbsoluteLinePosition(line, col)))
            .Or(from _ in Character.EqualTo('=')
                from col in ColumnParser
                select new EndPosition(new SameLinePosition(col)))
            .Or(from punct in PunctuationParser
                from col in ColumnParser
                select new EndPosition(new PunctuationLinePosition(punct, col)));

    static readonly TextParser<Position> PositionParser =
        Character.In(':', ';').Select(c => (Position)new SamePosition(c == ':' ? 1 : 2))
            .Or(from _ in Character.EqualTo('^')
                from col in ColumnParser
                select (Position)new SameLineEndColumn(col))
            .Or(from c in Character.In('<', '>')
                from col in ColumnParser
                select (Position)new SameLineStartColumn(col, c == '<' ? 1 : 2))
            .Or(from c in Character.In('[', ']')
                from col1 in ColumnParser
                from col2 in ColumnParser
                select (Position)(c == '[' ? new SameLineRange(col1, col2) : new NextLineRange(col1, col2)))
            .Or(from start in StartPositionParser
                from end in EndPositionParser
                select (Position)new FullRange(start, end));
    
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

    // Helper method to convert Superpower AST to Models
    static Models.Lexeme ConvertToModelLexeme(Lexeme superpowerLexeme, Dictionary<string, LexemeNameDefinition>? nameDefinitions, ref AbsolutePosition currentPosition)
    {
        // Look up name definition by base36 string
        LexemeNameDefinition? nameDefinition = null;
        nameDefinitions?.TryGetValue(superpowerLexeme.Radix36Number, out nameDefinition);

        // Convert content
        var modelContent = ConvertContent(superpowerLexeme.Content);

        // Convert position using the GetLexemePosition method
        var lexemeAbsolutePosition = superpowerLexeme.Position.GetLexemePosition(currentPosition);

        var modelPosition = new Models.Position(lexemeAbsolutePosition.StartLine, lexemeAbsolutePosition.StartColumn,
                                lexemeAbsolutePosition.EndLine, lexemeAbsolutePosition.EndColumn);

        // Update current position to the end of this lexeme for the next iteration
        currentPosition = lexemeAbsolutePosition;

        return new Models.Lexeme
        {
            Type = superpowerLexeme.Type.ToString(),
            NumberString = superpowerLexeme.Radix36Number,
            Position = modelPosition,
            Content = modelContent,
            NameDefinition = nameDefinition
        };
    }

    // Helper method to convert content
    static LexemeContent ConvertContent(Content superpowerContent) => superpowerContent switch
    {
        EmptyContent => LexemeContent.Empty,
        StringContent sc => new Models.StringContent(sc.Value),
        IntegerContent ic => new Models.NumberContent(ic.Value),
        FloatContent fc => new Models.NumberContent((long)fc.Value), // Convert float to long for now
        BooleanContent bc => new Models.BooleanContent(bc.Value),
        _ => LexemeContent.Empty
    };

    // Main parse method - without name definitions (for backward compatibility)
    public static Models.LexemeFile Parse(string input) => Parse(input, null);

    // Main parse method - with name definitions
    public static Models.LexemeFile Parse(string input, Dictionary<string, LexemeNameDefinition>? nameDefinitions)
    {
        var superpowerFile = FileParser.Parse(input);

        // Convert to Models types
        var header = new FileHeader(superpowerFile.Domain, superpowerFile.FileSourceInformation, superpowerFile.Encoding);

        // Track current position state while converting lexemes
        var currentPosition = new AbsolutePosition(Models.PositionConstants.InitialStartLine, Models.PositionConstants.InitialStartColumn, Models.PositionConstants.InitialEndLine, Models.PositionConstants.InitialEndColumn);
        var modelLexemes = superpowerFile.Lexemes.Select(superpowerLexeme => ConvertToModelLexeme(superpowerLexeme, nameDefinitions, ref currentPosition)).ToList();

        return new Models.LexemeFile(header, modelLexemes);
    }
}