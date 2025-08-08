# LexemeExtractor Architecture

## Project Structure

```
LexemeExtractor/
├── Program.cs                          # Entry point with command-line interface
├── LexemeExtractor.csproj             # Project configuration (.NET 9.0)
├── Models/
│   ├── LexemeFile.cs                  # Complete lexeme file representation
│   ├── Lexeme.cs                      # Individual lexeme with name definitions
│   ├── Position.cs                    # Line/column position information
│   ├── LexemeContent.cs               # Content variants (string, number, boolean, empty)
│   ├── FileHeader.cs                  # Domain, filename, encoding metadata
│   ├── LexemeNameDefinition.cs        # Name definition from companion .txt files
│   └── PositionConstants.cs           # Initial position values for parsing
├── Superpower/
│   ├── Parser.cs                      # Superpower-based parser with AST types
│   └── NameDefinitionParser.cs        # Parser for lexeme name definition files
├── OutputFormatters/
│   ├── ILexemeFormatter.cs            # Streaming formatter interface
│   ├── FormatterFactory.cs           # Factory for creating formatters
│   ├── StreamingFormatterBase.cs     # Base class for streaming formatters
│   ├── TextStreamingFormatter.cs     # Text output
│   ├── JsonStreamingFormatter.cs     # JSON output
│   ├── CsvStreamingFormatter.cs      # CSV output
│   └── XmlStreamingFormatter.cs      # XML output
└── Tests/
    ├── StreamingFormatterTests.cs    # Unit tests for formatters
    ├── StreamingIntegrationTest.cs   # End-to-end integration tests
    └── TestFiles/                    # Test programs and sample data
```

## Architecture Overview

### Data Flow

```
.lexemes file → Header Parser → Name Resolution → Line-by-Line Parser → Streaming Formatter → Output
                                      ↑
                               Name Definition Files (.txt)
```

1. **Input**: Compressed `.lexemes` files via glob patterns or stdin
2. **Header Parsing**: Parse file header (domain, filename, encoding) from first 3 lines
3. **Name Resolution**: Load companion `.txt` files based on domain for lexeme name definitions
4. **Streaming Parsing**: Parse lexemes one line at a time using Superpower combinators
5. **Model Conversion**: Convert each lexeme to domain model with position calculation
6. **Streaming Output**: Formatters process lexemes individually without building complete list
7. **Output**: Multiple formats (text, JSON, CSV, XML) to console or files

### Core Components

#### **Program.cs**
- Command-line argument parsing with `--format` option
- File globbing and stdin input handling
- Orchestrates parsing pipeline with error handling
- Integrates name definition loading and formatter selection

#### **Superpower Parser (Parser.cs)**
- Combinator-based parser using Superpower library
- Creates strongly-typed AST with position and content parsing
- Handles complex position encodings (relative, absolute, punctuation-based)
- Supports all lexeme types (A-O) and content variants

#### **AST Types**
- **File**: Domain, filename, encoding, and lexeme collection
- **Lexeme**: Type, radix36 number, position, and content
- **Position**: Abstract base with specialized implementations for different encodings
- **Content**: String, integer, float, boolean, or empty content variants

#### **Name Definition Parser**
- Parses companion `.txt` files with format: `name = :number TYPE;`
- Supports quoted and unquoted names, hex numbers, optional types
- Provides lexeme name resolution

#### **Streaming Output Formatters**
- **TextStreamingFormatter**: Text output
- **JsonStreamingFormatter**: JSON output
- **CsvStreamingFormatter**: CSV output
- **XmlStreamingFormatter**: XML output

## Key Algorithms

### Position Decoding System

The Superpower parser handles complex position encodings through a hierarchical system:

```csharp
public abstract record Position
{
    public abstract AbsolutePosition GetLexemePosition(AbsolutePosition currentPosition);
}
```

**Position Types:**
- **SamePosition**: Width-based positioning (`:` = 1 char, `;` = 2 chars)
- **SameLineEndColumn**: Column-relative positioning (`^`)
- **SameLineStartColumn**: Start column with width (`<`, `>`)
- **SameLineRange**: Column range on same line (`[`, `]`)
- **NextLineRange**: Column range on next line
- **FullRange**: Complete start/end position specification

**Column Encoding:**
- **AbsoluteColumn**: Direct column numbers
- **RelativeColumn**: Radix52 offsets (a-z = 27-52, A-Z = 1-26)
- **SameColumn**: Reuse previous column (`=`)

### Content Parsing

```csharp
static readonly TextParser<Content> ContentParser =
    StringContentParser.Select(s => (Content)new StringContent(s))
    .Or(from sign in Character.In('+', '-')
        from num in NumberParser
        select (Content)new IntegerContent(sign == '-' ? -num : num, sign))
    .Or(NumberParser.Select(num => (Content)new IntegerContent(num)))
    .Or(FloatParser.Select(f => (Content)new FloatContent(f)))
    .Or(Span.EqualTo("~t").Select(_ => (Content)new BooleanContent(true)))
    .Or(Span.EqualTo("~f").Select(_ => (Content)new BooleanContent(false)))
    .Or(Character.ExceptIn('\n', '\r').Many().Select(chars =>
        chars.Length == 0 ? (Content)new EmptyContent() : (Content)new StringContent(new string(chars))));
```

### Name Definition Resolution

```csharp
public static string GetDefinitionFilePath(string domain, string inputFilePath)
{
    var searchPaths = new[]
    {
        Path.GetDirectoryName(inputFilePath),
        Environment.GetEnvironmentVariable("LEXEME_NAMES_FILES"),
        Directory.GetCurrentDirectory(),
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    };
    
    return searchPaths
        .Where(path => !string.IsNullOrEmpty(path))
        .Select(path => Path.Combine(path, $"{domain}.txt"))
        .FirstOrDefault(File.Exists) ?? $"{domain}.txt";
}
```

## Dependencies

- **Superpower 3.1.0**: Combinator parser library
- **.NET 9.0**: Target framework
- **System.Text.Json**: Built-in JSON serialization

## Build Configuration

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net9.0</TargetFramework>
  <LangVersion>latestmajor</LangVersion>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <SelfContained>true</SelfContained>
  <PublishAot>true</PublishAot>
  <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
</PropertyGroup>
```

## Usage Examples

```bash
# Process single file with default text format
LexemeExtractor example.lexemes

# Process multiple files with JSON output
LexemeExtractor "*.lexemes" --format json

# Process from stdin with CSV format
cat file.lexemes | LexemeExtractor --format csv

# Process with XML output
LexemeExtractor "data/*.lexemes" --format xml
```

## Command-Line Interface

```
Usage: LexemeExtractor <glob-pattern> [--format <format>]
       LexemeExtractor [--format <format>] < input.lexemes

Formats: text (default), json, csv, xml

Examples:
  LexemeExtractor "*.lexemes" --format json
  cat file.lexemes | LexemeExtractor --format csv
```

## Testing Architecture

- **StreamingFormatterTests**: Unit tests for each output formatter
- **StreamingIntegrationTest**: End-to-end parsing and formatting tests
- **TestFiles/**: Sample programs demonstrating formatter usage
- **AOT Compatibility Tests**: Native compilation compatibility
