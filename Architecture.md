# LexemeExtractor Architecture

## Project Structure

```
LexemeExtractor/
├── Program.cs                          # Entry point with file globbing
├── LexemeExtractor.csproj             # Project configuration
├── Grammar/
│   ├── LexemeFormat.g4                # ANTLR grammar definition
│   └── Generated/                     # ANTLR-generated parser files
│       ├── LexemeFormatLexer.cs
│       ├── LexemeFormatParser.cs
│       ├── LexemeFormatBaseVisitor.cs
│       └── LexemeFormatListener.cs
├── Models/
│   ├── LexemeFile.cs                  # Represents complete .lexemes file
│   ├── Lexeme.cs                      # Individual lexeme data
│   ├── Position.cs                    # Line/column position
│   ├── LexemeContent.cs               # Content variants (string, number, etc.)
│   └── FileHeader.cs                  # Domain, filename, encoding info
├── Parsing/
│   ├── LexemeFileParser.cs            # High-level parser interface
│   ├── LexemeVisitor.cs               # ANTLR visitor implementation
│   ├── PositionDecoder.cs             # Decodes compressed positions
│   ├── ContentDecoder.cs              # Decodes lexeme content
│   └── EncodingHelper.cs              # Radix36, column encoding utilities
├── Output/
│   ├── IOutputFormatter.cs            # Output formatting interface
│   ├── ConsoleFormatter.cs            # Human-readable console output
│   ├── JsonFormatter.cs               # JSON output format
│   └── CsvFormatter.cs                # CSV output format
└── Exceptions/
    └── LexemeParseException.cs        # Custom parsing exceptions
```

## Architecture Overview

### Data Flow

```
.lexemes file → ANTLR Parser → Visitor → Models → Formatter → Output
```

1. **Input**: Compressed `.lexemes` files via glob patterns
2. **Parsing**: ANTLR grammar processes file structure and lexeme entries
3. **Decoding**: Custom visitors decode compressed positions and content
4. **Modeling**: Parsed data converted to strongly-typed objects
5. **Formatting**: Output formatters produce human-readable results
6. **Output**: Console display or file export

### Core Components

#### **Program.cs**
- File globbing and argument processing
- Orchestrates parsing and output for each file
- Error handling and user feedback

#### **ANTLR Grammar (LexemeFormat.g4)**
```antlr
grammar LexemeFormat;

file: header lexeme* EOF;
header: domain NEWLINE filename NEWLINE encoding NEWLINE;
lexeme: type radix36Number position content? NEWLINE;

position: shorthandPosition | fullPosition;
shorthandPosition: ':' | ';' | '@' | '|' | '_' | /* ... */;
fullPosition: startPosition endPosition;
```

#### **LexemeVisitor.cs**
- Implements ANTLR visitor pattern
- Converts parse tree to domain objects
- Handles position decoding and content parsing

#### **PositionDecoder.cs**
- Decodes compressed position encodings
- Maintains state for relative position calculations
- Handles special cases (`:`, `;`, `@`, `===A`, etc.)

#### **Output Formatters**
- **ConsoleFormatter**: Human-readable text output
- **JsonFormatter**: Structured JSON for tooling integration
- **CsvFormatter**: Tabular data for analysis

## Key Algorithms

### Position Decoding Algorithm

```csharp
public Position DecodePosition(string encoded, Position lastPosition, bool lineChanged)
{
    return encoded switch
    {
        ":" => new Position(lastPosition.Line, lastPosition.Column, 1),
        ";" => new Position(lastPosition.Line, lastPosition.Column, 2),
        "@" => lastPosition,
        "|" => lastPosition with { Column = lastPosition.Column + 1 },
        "_" => lastPosition with { Column = lastPosition.Column + 2 },
        "=" => lineChanged ? new Position(lastPosition.Line, 0) : lastPosition,
        var punct when IsPunctuation(punct) => DecodeLineIncrement(punct, lastPosition),
        var letter when IsLetter(letter) => DecodeColumnIncrement(letter, lastPosition),
        _ => DecodeFullPosition(encoded)
    };
}
```

### Content Decoding

```csharp
public LexemeContent DecodeContent(string content)
{
    return content switch
    {
        "" => LexemeContent.Empty,
        ['\"', .. var stringContent] => DecodeString(stringContent),
        ['+', .. var digits] => new NumberContent(ParseNumber(digits)),
        ['-', .. var digits] => new NumberContent(-ParseNumber(digits)),
        "~t" => new BooleanContent(true),
        "~f" => new BooleanContent(false),
        _ => DecodeNumericContent(content)
    };
}
```

## Output Formats

### Console Output Example
```
File: /temp/example.java (Java~~Java1_5)

Lexeme #1: Comment (Type: 0)
  Position: Line 2, Column 1-44
  Content: "/* Copyright 1997-1999 by Semantic Designs, Inc */"

Lexeme #2: Package (Type: 2a)
  Position: Line 3, Column 1-7
  Content: "package"

Lexeme #3: Identifier (Type: 2v)
  Position: Line 3, Column 9-13
  Content: "javax"
```

### JSON Output Example
```json
{
  "file": "/temp/example.java",
  "domain": "Java~~Java1_5",
  "lexemes": [
    {
      "type": "0",
      "number": 0,
      "position": { "startLine": 2, "startColumn": 1, "endLine": 2, "endColumn": 44 },
      "content": { "type": "string", "value": "/* Copyright 1997-1999 */" }
    }
  ]
}
```

## Dependencies

- **Antlr4.Runtime.Standard**: ANTLR parser runtime
- **System.Text.Json**: JSON output formatting
- **.NET 9.0**: Target framework with C# 13 features

## Build Configuration

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net9.0</TargetFramework>
  <PublishAot>true</PublishAot>
  <SelfContained>true</SelfContained>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Antlr4.Runtime.Standard" Version="4.13.1" />
  <PackageReference Include="Antlr4.CodeGenerator" Version="4.13.1" />
</ItemGroup>

<ItemGroup>
  <Antlr4 Include="Grammar\*.g4" />
</ItemGroup>
```

## Usage Examples

```bash
# Process single file
LexemeExtractor example.lexemes

# Process multiple files with glob
LexemeExtractor "*.lexemes"

# Output to JSON
LexemeExtractor --format json "*.lexemes"

# Process files in subdirectory
LexemeExtractor "data/*.lexemes"
```
