# LexemeExtractor File Format Specification

## Overview

This tool processes compressed `.lexemes` files to produce human-readable lexeme information. For a given input file `path/file.lexemes`, the tool outputs the decompressed lexeme data in a readable format.

## Input File Structure

Each `.lexemes` input file contains:

1. **Line 1**: Domain/dialect for the original source file
2. **Line 2**: Originating filename
3. **Remaining lines**: Compressed lexemes, one per line

## Lexeme Format

Each lexeme line follows this structure:

```
<lexeme_number> <start_line> <start_column> <end_line> <end_column> <content>
```

### Components

- **Lexeme Number**: Number assigned by DMS according to the lexical specification
- **Start Position**: Line and column where the lexeme begins
- **End Position**: Line and column where the lexeme ends (character past the end)
- **Content**: Variable value based on lexeme type

### Content Types

Content varies by lexeme type as defined by the DMS lexical specification:

- **Strings**: Encoded with starting `"` followed by Unicode content, ending with newline
- **Numbers**: Hex character codes, natural/integer values, or float values
- **Comments**: String containing comment start/end characters as extracted by DMS lexer

**String Encoding**: Files are encoded as UTF-8 or Unicode. Most Unicode characters are represented directly. Escape sequences use `"` followed by hex character codes for special characters (newlines, quotes within strings, line-break Unicode characters).

## Compression Format

The lexeme entries use special encoding to minimize file size. LexemeExtractor must decode these compressed representations to produce readable output.

### Lexeme Number Encoding

- **Radix 36**: Uses digits 0-9 and letters a-z
- **Special case**: Number 0 represents comment tokens (instead of EOF lexemes)

### Line Number Encoding

Line numbers can be encoded as:

1. **Decimal numbers**: Standard unsigned decimal representation
2. **Compressed encoding**: Single punctuation characters for common patterns
   - `=`: Same line number as previous lexeme (line 1 at file start)
   - `!"...` (codes #21-#2F): Previous line number + 1-15 (punctuation code - #20)

**Common usage**: Characters `=`, `!`, and `"` are frequently used since line numbers typically change by 0, 1, or 2 between lexemes.

### Column Number Encoding

Column numbers can be encoded as:

1. **Decimal numbers**: Standard unsigned decimal representation
2. **Compressed encoding**: Characters #41-#7E (A-Z, a-z, punctuation)
   - `=`: Same column as previously seen (0 if line number changed)
   - **Line changed**: Encoded value = new column number
   - **Same line**: Encoded value = increment to last column number

**Common usage**: Letters A-H frequently appear in column fields for typical source code.

### Special Case Optimizations

Additional compression patterns provide 25-50% average size reduction:

| Pattern | Shorthand | Description |
|---------|-----------|-------------|
| `===A` | `:` | 1-character lexeme |
| `===B` | `;` | 2-character lexeme |
| `===<letter>` | `^<letter>` | Same position, ends at letter |
| `===<number>` | `^<number>` | Same position, ends at number |
| `=<letter>=A` | `< <letter>` | Same line, letter column, 1 char |
| `=<number>=A` | `< <number>` | Same line, number column, 1 char |
| `=<letter>=B` | `> <letter>` | Same line, letter column, 2 chars |
| `=<number>=B` | `> <number>` | Same line, number column, 2 chars |
| `=<letter>=<letter>` | `[<letter><letter>` | Same line, letter to letter |
| `=<number>=<letter>` | `[<number><letter>` | Same line, number to letter |
| `=<letter>=<number>` | `[<letter><number>` | Same line, letter to number |
| `!<letter>=<letter>` | `]<letter><letter>` | Next line, letter to letter |
| `!<number>=<letter>` | `]<number><letter>` | Next line, number to letter |
| `!<letter>=<number>` | `]<letter><number>` | Next line, letter to number |
| `==` | `@` | Same line/column as last |
| `=A` | `|` | Same line, column + 1 |
| `=B` | `_` | Same line, column + 2 |

## Grammar Specification

```ebnf
file = DOMAIN NEWLINE FILESOURCEINFORMATION NEWLINE ENCODING NEWLINE lexeme* ;

lexeme = type RADIX36NUMBER position content NEWLINE ;

type = [A-O] ;  (* 16 extra codes associated with token *)

RADIX36NUMBER = [0-9a-z]+ ;

position = ":"                          (* same line/column; 1 character wide *)
         | ";"                          (* same line/column; 2 characters wide *)
         | "^" column                   (* same line/column; ends at column *)
         | "<" column                   (* same line, specified column; 1 char *)
         | ">" column                   (* same line, specified column; 2 chars *)
         | "[" column column            (* same line, specified column/width *)
         | "]" column column            (* next line, specified column/width *)
         | start_position end_position  (* explicit start/end positions *)
         ;

start_position = NUMBER column          (* absolute line number *)
               | "=" column             (* same line as last *)
               | punctuation column     (* line + punctuation index *)
               | encoded_position
               ;

end_position = NUMBER column            (* absolute line number *)
             | "=" column               (* same line as last *)
             | punctuation column       (* line + punctuation index *)
             | encoded_position
             ;

column = NUMBER                         (* absolute column number *)
       | [A-Za-z]                       (* last column + radix52 digit + 1 *)
       | "="                            (* same as last column *)
       ;

encoded_position = "@"                  (* same line/column as last *)
                 | "|"                  (* same line, column + 1 *)
                 | "_"                  (* same line, column + 2 *)
                 ;

punctuation = [!"#$%&'()*+,-./ ]        (* index establishes line increment *)

content = ""                            (* no content *)
        | "\"" STRINGCONTENT            (* string content *)
        | " " NUMBER                    (* space + number *)
        | "+" NUMBER                    (* positive number *)
        | "-" NUMBER                    (* negative number *)
        | [+-]? FLOAT                   (* floating point *)
        | "~t"                          (* true *)
        | "~f"                          (* false *)
        ;
```

## Example Input

The following shows a sample `.lexemes` input file that LexemeExtractor would process. Note the frequent occurrence of compressed position encodings like `===A` and `=A=E`.

```
Java~~Java1_5
/temp/ugly.java
0!A,C"/*"000d"000a* Copyright 1997-1999 by Semantic Designs, Inc, <more text deleted> */
2a"A=G
2v=A=E"javax
k===A
2v===E"swing
o===A
22!A=F
2v=A=D"java
k===A
2v===C"awt
k===A
b===A
o===A
22=A=F
2v=A=D"java
...
```

### Input Format Explanation

- **Line 1**: `Java~~Java1_5` - Domain/dialect specification
- **Line 2**: `/temp/ugly.java` - Original source filename
- **Line 3+**: Compressed lexeme entries that need to be decoded
- **Compression patterns**: `===A` (single character), `=A=E` (same line, different columns)

## Expected Output

LexemeExtractor should decode this compressed format into human-readable information showing:
- Lexeme type and number
- Actual line and column positions (decoded from compressed format)
- Lexeme content (strings, numbers, identifiers)
- Clear formatting for easy analysis