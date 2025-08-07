#!/bin/bash

echo "=== Column Offset Analysis ==="
echo

cd LexemeExtractor || { echo "LexemeExtractor directory not found!"; exit 1; }

echo "Working directory: $(pwd)"
echo

# Create a test file with first 50 lexemes to avoid parsing errors
echo "Creating test file with first 50 lexemes..."
head -53 SampleInput/SRT1000.CBL.lexemes > test_analysis.lexemes
echo "Created test file"

# Run lexeme extractor
echo
echo "Running lexeme extractor..."
/home/chaz/.dotnet/dotnet run -- --format text test_analysis.lexemes

if [ $? -ne 0 ]; then
    echo "Error running extractor"
    exit 1
fi

echo "Lexeme extraction completed successfully"
echo

# Show the output
echo "=== Extractor Output ==="
cat test_analysis.lexemes.txt
echo
echo "=== End Output ==="

echo
echo "Now let's manually check some string lexemes..."

# Check line 3 for "SRT1000"
echo
echo "Checking line 3 for 'SRT1000':"
echo "Source line 3:"
sed -n '3p' SampleInput/SRT1000.CBL | cat -n
echo
echo "Position of 'SRT1000' in line 3:"
sed -n '3p' SampleInput/SRT1000.CBL | grep -o -b 'SRT1000'
echo "(This shows byte offset:string - add 1 for 1-based column)"

# Check for other string lexemes in the output
echo
echo "String lexemes found in output:"
grep '= "' test_analysis.lexemes.txt | head -5

echo
echo "Analysis complete!"
