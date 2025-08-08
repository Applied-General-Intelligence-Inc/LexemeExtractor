# LexemeExtractor

A .NET 9.0 console application that decompresses lexeme files from Semantic Designs and generates formatted output. The application supports multiple output formats and can process files via glob patterns or stdin.

## Features

- **Lexeme File Decompression**: Decompresses and decodes lexeme files from Semantic Designs
- **Multiple Output Formats**: Supports text, JSON, CSV, and XML output formats
- **Glob Pattern Matching**: Process multiple lexeme files using standard glob patterns
- **Stdin Support**: Process lexeme files via piped input
- **Linux Path Support**: Support for `~/` home directory expansion on Linux
- **Name Definition Support**: Automatically loads human-readable lexeme names from companion definition files
- **Cross-Platform**: Built for .NET 9.0 with Linux x64 runtime
- **AOT Compilation**: Configured for Ahead-of-Time compilation

## Usage

```bash
LexemeExtractor <glob-pattern> [OPTIONS]
LexemeExtractor [OPTIONS] < input.lexemes
```

### Options

- `--format <format>`: Output format: text, json, csv, xml (default: text)
- `--help, -h`: Show help message
- `--version, -v`: Show version information

### Name Definition Files

A name definition file for the lexer domain is required. This file must be a .txt file named with the same name that is provided at the top of each lexeme file (the domain/dialect name). For example, if the lexeme file contains `COBOL~IBMEnterprise` as the domain, the application will look for `COBOL~IBMEnterprise.txt`. The application searches for name definition files in the following order:

1. Same directory as the input file
2. Directory specified by `LEXEME_NAMES_FILES` environment variable
3. Program's current directory
4. Executable's directory

The name definition file should contain lexeme definitions in the format:
```
name = :number TYPE;
```

Where names may have quotes, numbers are in base16 with colon prefix, and types are optional.

### Examples

```bash
# Process all lexeme files in current directory
LexemeExtractor "*.lexemes"

# Process lexeme files with JSON output
LexemeExtractor "data/*.lex" --format json

# Process a single lexeme file via pipe
cat file.lexemes | LexemeExtractor --format csv

# Process lexeme files in home directory with XML output
LexemeExtractor ~/documents/*.lexemes --format xml

# Pipe through less for viewing large outputs
cat large.lexemes | LexemeExtractor | less
```

## Building

### Prerequisites

- .NET 9.0 SDK or later
- Linux x64 environment (for the current configuration)

### Build Commands

```bash
# Standard build
dotnet build

# Release build
dotnet build -c Release

# Publish AOT binary
dotnet publish -c Release
```

## Running

```bash
# Run with dotnet
dotnet run --project LexemeExtractor -- "*.lexemes"

# Run compiled binary (after publish)
./LexemeExtractor/bin/Release/net9.0/linux-x64/publish/LexemeExtractor "*.lexemes"
```

## Technical Details

### Project Configuration

- **Target Framework**: .NET 9.0
- **Language Version**: Latest Major
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **AOT Compilation**: Enabled
- **Self-contained**: True
- **Runtime Identifier**: linux-x64

## Architecture

The application follows a simple, functional approach:

1. **Argument Validation**: Ensures a glob pattern is provided or stdin is available
2. **Path Expansion**: Handles `~/` expansion for Linux home directories
3. **Pattern Separation**: Splits directory path from file pattern
4. **File Discovery**: Uses `Directory.GetFiles()` for pattern matching
5. **File Processing**: Decompresses lexeme files and generates formatted output

## Documentation

- **[LexemeFileFormat.md](LexemeFileFormat.md)**: Detailed specification of the lexeme file format and encoding
- **[LexemeNameDefinitions.md](LexemeNameDefinitions.md)**: Documentation for lexeme name definition files

## Contributing

This project uses C# coding standards:

- Type inference with `var` wherever possible
- Nullable reference types enabled
- .NET 9.0 language features
- Functional-style code

## License

[Add your license information here]

## Organization

Developed by Applied General Intelligence Inc.
