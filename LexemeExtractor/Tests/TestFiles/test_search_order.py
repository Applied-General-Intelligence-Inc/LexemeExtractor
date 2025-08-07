#!/usr/bin/env python3
"""
Test script to verify the lexeme name definition file search order.
This simulates the search behavior without needing to compile C#.
"""

import os
import tempfile
from pathlib import Path

def test_search_order():
    print("Testing Lexeme Name Definition File Search Order")
    print("=" * 50)
    
    domain = "TestDomain"
    filename = f"{domain}.txt"
    
    # Create temporary directories for testing
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)
        
        # Create test directories
        input_dir = temp_path / "input"
        current_dir = temp_path / "current" 
        env_dir = temp_path / "env"
        exec_dir = temp_path / "exec"
        
        for d in [input_dir, current_dir, env_dir, exec_dir]:
            d.mkdir()
        
        # Create test files with different content
        (input_dir / filename).write_text("# Input directory file")
        (current_dir / filename).write_text("# Current directory file")
        (env_dir / filename).write_text("# Environment directory file")
        (exec_dir / filename).write_text("# Executable directory file")
        
        # Test search order simulation
        def simulate_search(lexeme_file_path, current_dir_path, env_var_path=None, exec_path=None):
            search_dirs = []
            
            # 1. Same directory as input file
            input_directory = os.path.dirname(lexeme_file_path)
            if input_directory:
                search_dirs.append(input_directory)
            
            # 2. Current directory
            search_dirs.append(current_dir_path)
            
            # 3. Environment variable directory
            if env_var_path and os.path.exists(env_var_path):
                search_dirs.append(env_var_path)
            
            # 4. Executable directory
            if exec_path:
                exec_directory = os.path.dirname(exec_path)
                if exec_directory:
                    search_dirs.append(exec_directory)
            
            # Search in order
            for directory in search_dirs:
                candidate_path = os.path.join(directory, filename)
                if os.path.exists(candidate_path):
                    return candidate_path, directory
            
            # Not found
            return None, None
        
        # Test scenarios
        scenarios = [
            {
                "name": "All directories have file - should find in input dir",
                "lexeme_path": str(input_dir / "test.lexemes"),
                "current": str(current_dir),
                "env": str(env_dir),
                "exec": str(exec_dir / "program.exe"),
                "expected_dir": str(input_dir)
            },
            {
                "name": "No file in input dir - should find in current dir",
                "lexeme_path": str(temp_path / "other" / "test.lexemes"),
                "current": str(current_dir),
                "env": str(env_dir),
                "exec": str(exec_dir / "program.exe"),
                "expected_dir": str(current_dir)
            },
            {
                "name": "Only in env dir - should find there",
                "lexeme_path": str(temp_path / "other" / "test.lexemes"),
                "current": str(temp_path / "empty1"),
                "env": str(env_dir),
                "exec": str(temp_path / "empty2" / "program.exe"),
                "expected_dir": str(env_dir)
            },
            {
                "name": "Only in exec dir - should find there",
                "lexeme_path": str(temp_path / "other" / "test.lexemes"),
                "current": str(temp_path / "empty1"),
                "env": str(temp_path / "empty2"),
                "exec": str(exec_dir / "program.exe"),
                "expected_dir": str(exec_dir)
            }
        ]
        
        for scenario in scenarios:
            print(f"\nScenario: {scenario['name']}")
            found_path, found_dir = simulate_search(
                scenario["lexeme_path"],
                scenario["current"],
                scenario.get("env"),
                scenario.get("exec")
            )
            
            if found_path:
                content = Path(found_path).read_text().strip()
                print(f"  Found: {found_path}")
                print(f"  Content: {content}")
                print(f"  Expected dir: {scenario['expected_dir']}")
                print(f"  Found dir: {found_dir}")
                print(f"  ✅ Correct!" if found_dir == scenario["expected_dir"] else "  ❌ Wrong directory!")
            else:
                print(f"  ❌ Not found!")

if __name__ == "__main__":
    test_search_order()
