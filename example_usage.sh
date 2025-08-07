#!/bin/bash

# Example usage of LexemeExtractor with lexeme name definitions

echo "LexemeExtractor - Lexeme Name Definitions Example"
echo "================================================="

# Set up environment variable for centralized definitions
export LEXEME_NAMES_FILES="./definitions"

# Create example directories
mkdir -p definitions
mkdir -p examples

# Create a sample definition file
cat > definitions/COBOL~IBMEnterprise.txt << 'EOF'
# COBOL lexeme name definitions
# Example definitions for demonstration

large_unsigned_integer_number = :20b RATIONAL;
exec_record_identifier = :248 STRING;
'PREFIX' = :97;
program_name = :1a2 IDENTIFIER;
'WORKING-STORAGE' = :2c4;
'DATA' = :2d5;
'DIVISION' = :2e6;
variable_name = :3f7 IDENTIFIER;
numeric_literal = :408 NUMERIC;
string_literal = :419 STRING;
comment_line = :42a COMMENT;
EOF

echo "Created definition file: definitions/COBOL~IBMEnterprise.txt"
echo "Set LEXEME_NAMES_FILES environment variable to: $LEXEME_NAMES_FILES"
echo ""

# Show the definition file content
echo "Definition file contents:"
echo "------------------------"
cat definitions/COBOL~IBMEnterprise.txt
echo ""

# Test the search order
echo "Testing search order:"
echo "1. Same directory as lexeme file"
echo "2. Current directory"  
echo "3. LEXEME_NAMES_FILES directory: $LEXEME_NAMES_FILES"
echo "4. Executable directory"
echo ""

# If the LexemeExtractor binary exists, run it
if [ -f "./LexemeExtractor/bin/Release/net9.0/linux-x64/SDLexemeDecoder" ]; then
    echo "Running LexemeExtractor with sample file..."
    ./LexemeExtractor/bin/Release/net9.0/linux-x64/SDLexemeDecoder LexemeExtractor/SampleInput/IND2000.cbl.lexemes
else
    echo "LexemeExtractor binary not found. Build the project first with:"
    echo "  dotnet build -c Release"
fi

echo ""
echo "Example completed!"
echo ""
echo "To use lexeme name definitions:"
echo "1. Create a definition file named {domain}.txt"
echo "2. Place it in one of the search directories"
echo "3. Run LexemeExtractor on your lexeme files"
echo "4. The output will include human-readable names"
