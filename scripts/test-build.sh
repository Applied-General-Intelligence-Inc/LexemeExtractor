#!/bin/bash

# Test script to validate local builds for different platforms
# This simulates what the GitHub Actions workflow will do

set -e

PROJECT_PATH="LexemeExtractor/LexemeExtractor.csproj"
OUTPUT_DIR="./test-builds"

echo "Testing local builds for LexemeExtractor..."

# Clean previous builds
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Test Linux x64 build (should work on this platform)
echo "Building for Linux x64..."
~/.dotnet/dotnet publish "$PROJECT_PATH" \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output "$OUTPUT_DIR/linux-x64"

# Verify the executable was created
if [ -f "$OUTPUT_DIR/linux-x64/LexemeExtractor" ]; then
    echo "✅ Linux x64 build successful"
    ls -la "$OUTPUT_DIR/linux-x64/LexemeExtractor"
else
    echo "❌ Linux x64 build failed"
    exit 1
fi

# Test that the executable runs
# Test that the executable runs and shows help
echo "Testing executable help..."
if "$OUTPUT_DIR/linux-x64/LexemeExtractor" --help > /dev/null 2>&1; then
    echo "✅ Executable --help works correctly"
else
    echo "❌ Executable --help failed"
    exit 1
fi

# Test version flag
echo "Testing executable version..."
if "$OUTPUT_DIR/linux-x64/LexemeExtractor" --version > /dev/null 2>&1; then
    echo "✅ Executable --version works correctly"
else
    echo "❌ Executable --version failed"
    exit 1
fi

# Test error handling
echo "Testing error handling..."
if "$OUTPUT_DIR/linux-x64/LexemeExtractor" > /dev/null 2>&1; then
    echo "❌ Executable should have failed with no arguments"
    exit 1
else
    echo "✅ Executable correctly shows error with no arguments"
fi

echo "Local build test completed successfully!"
echo "The GitHub Actions workflow should work correctly."
