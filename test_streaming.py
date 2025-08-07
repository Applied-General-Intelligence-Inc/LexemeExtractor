#!/usr/bin/env python3
"""
Simple test script to verify streaming formatters work correctly
"""

import subprocess
import sys
import os

def run_test(format_type, input_file):
    """Run a test with the specified format"""
    print(f"\n=== Testing {format_type.upper()} format ===")
    
    try:
        # Test with file input
        result = subprocess.run([
            'dotnet', 'run', '--project', 'LexemeExtractor',
            '--format', format_type, input_file
        ], capture_output=True, text=True, timeout=30)
        
        if result.returncode != 0:
            print(f"❌ File test failed for {format_type}")
            print(f"stdout: {result.stdout}")
            print(f"stderr: {result.stderr}")
            return False
        
        print(f"✅ File test passed for {format_type}")
        
        # Test with stdin
        with open(input_file, 'r') as f:
            content = f.read()
        
        result = subprocess.run([
            'dotnet', 'run', '--project', 'LexemeExtractor',
            '--format', format_type
        ], input=content, capture_output=True, text=True, timeout=30)
        
        if result.returncode != 0:
            print(f"❌ Stdin test failed for {format_type}")
            print(f"stdout: {result.stdout}")
            print(f"stderr: {result.stderr}")
            return False
        
        print(f"✅ Stdin test passed for {format_type}")
        print(f"Sample output:\n{result.stdout[:200]}...")
        
        return True
        
    except subprocess.TimeoutExpired:
        print(f"❌ Test timed out for {format_type}")
        return False
    except Exception as e:
        print(f"❌ Test error for {format_type}: {e}")
        return False

def main():
    """Main test function"""
    print("Testing Streaming Output Formatters")
    print("=" * 40)
    
    input_file = "test_sample.lexemes"
    if not os.path.exists(input_file):
        print(f"❌ Test file {input_file} not found")
        return False
    
    formats = ['text', 'json', 'csv', 'xml']
    all_passed = True
    
    for fmt in formats:
        if not run_test(fmt, input_file):
            all_passed = False
    
    if all_passed:
        print("\n🎉 All streaming formatter tests passed!")
        return True
    else:
        print("\n💥 Some tests failed!")
        return False

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
