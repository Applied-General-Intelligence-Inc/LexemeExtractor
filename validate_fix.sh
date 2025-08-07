#!/bin/bash

echo "=== Comprehensive Column Fix Validation ==="
echo

cd LexemeExtractor || { echo "LexemeExtractor directory not found!"; exit 1; }

echo "Testing the column offset fix..."
echo

# Run the extractor on our test file
/home/chaz/.dotnet/dotnet run -- --format text test_analysis.lexemes > /dev/null 2>&1

echo "Checking specific string lexemes against source:"
echo "=============================================="

# Check SRT1000 on line 3
echo "1. SRT1000 on line 3:"
echo "   Expected: column 21 (byte offset 20)"
echo "   Source verification:"
sed -n '3p' SampleInput/SRT1000.CBL | grep -o -b 'SRT1000'
echo "   Extractor output:"
grep "SRT1000" test_analysis.lexemes.txt
echo

# Check first RCTTRAN on line 11
echo "2. First RCTTRAN on line 11:"
echo "   Expected: columns 19-25 (byte offset 18-24)"
echo "   Source verification:"
sed -n '11p' SampleInput/SRT1000.CBL | grep -o -b 'RCTTRAN' | head -1
echo "   Extractor output:"
grep "RCTTRAN" test_analysis.lexemes.txt | head -1
echo

# Check second RCTTRAN on line 11
echo "3. Second RCTTRAN on line 11:"
echo "   Expected: columns 38-44 (byte offset 37-43)"
echo "   Source verification:"
sed -n '11p' SampleInput/SRT1000.CBL | grep -o -b 'RCTTRAN' | tail -1
echo "   Extractor output:"
grep "RCTTRAN" test_analysis.lexemes.txt | tail -1
echo

# Summary
echo "Summary of Issues Found:"
echo "======================="

# Check SRT1000
srt_expected=21
srt_actual=$(grep "SRT1000" test_analysis.lexemes.txt | sed -n 's/.*Column \([0-9]*\)-.*/\1/p')
if [ "$srt_actual" = "$srt_expected" ]; then
    echo "✓ SRT1000: CORRECT (column $srt_actual)"
else
    echo "✗ SRT1000: INCORRECT - expected $srt_expected, got $srt_actual (offset: $((srt_actual - srt_expected)))"
fi

# Check first RCTTRAN
rcttran1_expected=19
rcttran1_actual=$(grep "RCTTRAN" test_analysis.lexemes.txt | head -1 | sed -n 's/.*Column \([0-9]*\)-.*/\1/p')
if [ "$rcttran1_actual" = "$rcttran1_expected" ]; then
    echo "✓ First RCTTRAN: CORRECT (column $rcttran1_actual)"
else
    echo "✗ First RCTTRAN: INCORRECT - expected $rcttran1_expected, got $rcttran1_actual (offset: $((rcttran1_actual - rcttran1_expected)))"
fi

# Check second RCTTRAN
rcttran2_expected=38
rcttran2_actual=$(grep "RCTTRAN" test_analysis.lexemes.txt | tail -1 | sed -n 's/.*Column \([0-9]*\)-.*/\1/p')
if [ "$rcttran2_actual" = "$rcttran2_expected" ]; then
    echo "✓ Second RCTTRAN: CORRECT (column $rcttran2_actual)"
else
    echo "✗ Second RCTTRAN: INCORRECT - expected $rcttran2_expected, got $rcttran2_actual (offset: $((rcttran2_actual - rcttran2_expected)))"
fi

echo
echo "Validation complete!"

# Show the pattern of offsets
echo
echo "Pattern Analysis:"
echo "================"
echo "If all offsets are consistent, we can identify the systematic error."
