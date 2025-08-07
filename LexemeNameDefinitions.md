# Lexeme Name Definitions

The LexemeExtractor supports automatic loading of human-readable names for lexemes through companion definition files. This feature enriches the output by providing meaningful names instead of just numeric identifiers.

## Definition File Format

Lexeme name definition files use the following format:

```
name = :hex_number [TYPE];
```

Where:
- `name` - The human-readable name (optionally quoted with single quotes)
- `hex_number` - The lexeme identifier in hexadecimal (without the colon)
- `TYPE` - Optional data type specification
- `;` - Optional semicolon terminator

### Examples

```
large_unsigned_integer_number = :20b RATIONAL;
exec_record_identifier = :248 STRING;
'PREFIX' = :97;
program_name = :1a2 IDENTIFIER;
'WORKING-STORAGE' = :2c4;
```

## File Search Order

When processing a lexeme file, the system searches for definition files in the following order:

1. **Same directory as the lexeme file** - `{domain}.txt` in the same directory
2. **Current working directory** - `{domain}.txt` in the program's current directory  
3. **Environment variable directory** - `{domain}.txt` in the directory specified by `LEXEME_NAMES_FILES`
4. **Executable directory** - `{domain}.txt` in the directory containing the program executable

The first file found in this search order will be used.

## Environment Variable

Set the `LEXEME_NAMES_FILES` environment variable to specify a directory containing lexeme name definition files:

```bash
export LEXEME_NAMES_FILES="/path/to/lexeme/definitions"
```

This is useful for maintaining a centralized repository of lexeme definitions that can be shared across multiple projects.

## Domain-Based Naming

Definition files are named after the domain specified in the lexeme file header. For example:

- Lexeme file header: `COBOL~IBMEnterprise`
- Definition file: `COBOL~IBMEnterprise.txt`

## Usage Examples

### Basic Usage
```bash
# Process lexeme file - automatically loads definitions if available
./LexemeExtractor sample.lexemes
```

### With Environment Variable
```bash
# Set environment variable for centralized definitions
export LEXEME_NAMES_FILES="/usr/local/share/lexeme-definitions"
./LexemeExtractor sample.lexemes
```

### Directory Structure Example
```
project/
├── data/
│   ├── sample.lexemes              # Domain: COBOL~IBMEnterprise
│   └── COBOL~IBMEnterprise.txt     # Definition file (same directory)
├── definitions/
│   └── Java~1_8.txt               # Centralized definition
└── LexemeExtractor
```

## Output Enhancement

When definition files are available, the output will show human-readable names:

**Without definitions:**
```
Lexeme #523: (Type: A) at Line 1, Column 5
```

**With definitions:**
```
Lexeme #523: large_unsigned_integer_number (Type: A) at Line 1, Column 5
```

## Comments and Formatting

Definition files support:
- **Comments**: Lines starting with `#` or `//` are ignored
- **Empty lines**: Blank lines are ignored
- **Flexible whitespace**: Spaces around `=` and `:` are optional
- **Optional semicolons**: The trailing `;` is optional

### Example with Comments
```
# COBOL lexeme definitions
# Generated from IBM Enterprise COBOL specification

large_unsigned_integer_number = :20b RATIONAL;
exec_record_identifier = :248 STRING;

# Keywords
'PREFIX' = :97;
'WORKING-STORAGE' = :2c4;
```

## Error Handling

- **Missing definition files**: The system continues processing without definitions
- **Malformed lines**: Individual malformed lines are skipped with error messages
- **Invalid hex numbers**: Lines with invalid hexadecimal numbers are skipped
- **Duplicate names**: Later definitions override earlier ones

## Integration

The lexeme name definition system is automatically integrated into all parsing methods:

- File-based parsing (`ParseFile`)
- Text-based parsing (`ParseText`) 
- Line-based parsing (`ParseLines`)
- Streaming parsing (via `Program.cs`)

No additional configuration is required - simply place the definition files in the appropriate location and they will be automatically loaded and used.
