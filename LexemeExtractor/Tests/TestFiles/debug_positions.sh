#!/bin/bash

echo "=== Detailed Position Analysis ==="
echo

cd LexemeExtractor || { echo "LexemeExtractor directory not found!"; exit 1; }

echo "Let's trace through the position calculations step by step..."
echo

# Look at the first few lexemes and their position encodings
echo "First 10 lexemes with their raw position encodings:"
echo "=================================================="
head -13 test_analysis.lexemes | tail -10 | nl

echo
echo "Now let's see what the extractor produces:"
echo "=========================================="
head -20 test_analysis.lexemes.txt | tail -10

echo
echo "Let's manually check the COBOL source for verification:"
echo "======================================================"

echo "Line 11 of COBOL source:"
sed -n '11p' SampleInput/SRT1000.CBL | cat -A
echo

echo "Character positions in line 11:"
sed -n '11p' SampleInput/SRT1000.CBL | sed 's/./&\n/g' | nl -v0

echo
echo "Let's find all occurrences of RCTTRAN in line 11:"
sed -n '11p' SampleInput/SRT1000.CBL | grep -o -b 'RCTTRAN'

echo
echo "Analysis of the position encoding '[AG':"
echo "========================================"
echo "[ = bracket pattern (same line, range)"
echo "A = 0x41 = 65 decimal"
echo "G = 0x47 = 71 decimal"
echo
echo "Current decoding:"
echo "A: 65 - 65 + 1 = 1"
echo "G: 71 - 65 + 1 = 7"
echo "So [AG should mean columns 1-7"
echo
echo "But RCTTRAN is actually at:"
echo "First occurrence: byte 18 = column 19"
echo "Second occurrence: byte 37 = column 38"
echo
echo "This suggests the character encoding might be 0-based, not 1-based"
echo "Or it might be an offset from the previous position"

echo
echo "Let's check what the previous position was..."
echo "Looking at lexemes before the first RCTTRAN:"

# Show the context around the RCTTRAN lexemes
echo
echo "Lexemes around first RCTTRAN (line 20 in lexeme file):"
sed -n '18,22p' test_analysis.lexemes | nl -v18

echo
echo "Corresponding output:"
grep -A2 -B2 "Column 20-27" test_analysis.lexemes.txt
