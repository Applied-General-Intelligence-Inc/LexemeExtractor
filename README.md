# LexemeExtractor

A .NET 9.0 console application that processes files matching glob patterns with support for Linux path conventions.

## Features

- **Glob Pattern Matching**: Process multiple files using standard glob patterns (e.g., `*.txt`, `*.cs`)
- **Linux Path Support**: Support for `~/` home directory expansion on Linux
- **C# 13 Language Features**: Uses list patterns, switch expressions, and type inference
- **Cross-Platform**: Built for .NET 9.0 with Linux x64 runtime
- **AOT Compilation**: Configured for Ahead-of-Time compilation

## Usage

```bash
LexemeExtractor <glob-pattern>
```

### Examples

```bash
# Process all text files in current directory
LexemeExtractor "*.txt"

# Process all C# files in a subdirectory
LexemeExtractor "src/*.cs"

# Process files in home directory
LexemeExtractor "~/Documents/*.log"

# Process files with absolute path
LexemeExtractor "/var/log/*.log"
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
dotnet run --project LexemeExtractor -- "*.txt"

# Run compiled binary (after publish)
./LexemeExtractor/bin/Release/net9.0/linux-x64/publish/LexemeExtractor "*.txt"
```

## Technical Details

### C# Language Features Used

- **List Pattern Matching**: For home directory expansion (`['~', '/', .. var rest]`)
- **Switch Expressions**: For path processing and directory resolution
- **Type Inference**: Use of `var` for type inference
- **Expression-bodied Methods**: Concise method definitions
- **Top-level Programs**: Simplified program structure

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

1. **Argument Validation**: Ensures a glob pattern is provided
2. **Path Expansion**: Handles `~/` expansion for Linux home directories
3. **Pattern Separation**: Splits directory path from file pattern
4. **File Discovery**: Uses `Directory.GetFiles()` for pattern matching
5. **File Processing**: Calls `ProcessFile()` method for each matching file

## Documentation

- **[LexemeExtractor.md](LexemeExtractor.md)**: Detailed specification of the lexeme file format and encoding

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
