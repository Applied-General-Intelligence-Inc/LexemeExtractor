import re

print("Testing Lexeme Name Definition Parsing")
print("=====================================")

# Test parsing individual lines with a regex
test_lines = [
    "large_unsigned_integer_number = :20b RATIONAL;",
    "exec_record_identifier = :248 STRING;",
    "'PREFIX' = :97;",
    "program_name = :1a2 IDENTIFIER;",
    "'WORKING-STORAGE' = :2c4;"
]

# Regex pattern to match: optional_quotes_name = :hex_number optional_type;
pattern = re.compile(r"^(?:'([^']+)'|([^=\s]+))\s*=\s*:([0-9A-Fa-f]+)(?:\s+([^;]+))?\s*;?\s*$")

print("Testing individual line parsing:")
for line in test_lines:
    match = pattern.match(line)
    if match:
        # Extract name (either quoted or unquoted)
        name = match.group(1) if match.group(1) else match.group(2)
        
        # Extract hex number and convert to decimal
        hex_number = match.group(3)
        number = int(hex_number, 16)
        
        # Extract optional data type
        data_type = match.group(4).strip() if match.group(4) and match.group(4).strip() else None
        
        print(f"  {line}")
        print(f"    -> Name: '{name}', Number: {number} (0x{number:X}), Type: '{data_type or '(no type)'}'")
    else:
        print(f"  {line} -> FAILED TO PARSE")

print("\nTest completed.")
