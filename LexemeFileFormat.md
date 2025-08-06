# LexemeExtractor File Format Specification

## Description

This tool processes a list of files to produce their lexemes. For a given path/file, the lexemes are placed in path/file.lexemes. The first line contains the domain/dialect for the file. The second line contains the originating filename; this probably is unnecessary. All further lines contain lexemes, one per line. The format of the lexeme information is as follows:

```
<lexeme number> <lexeme start line number> <lexeme start column number>
     <lexeme end line number> <lexeme end column number>  <lexeme content>
```

The lexeme number is the number assigned by DMS to a lexeme according to the DMS lexical specification of the domain/dialect.

Lexeme start and end information indicates where the first and character past the end of the lexeme, respectively are found in the source file.

Lexeme content contains any variable value (hex character code, natural or integer value, float value, or string content, depending on the lexeme type as defined by the DMS lexical specification. Strings are encoded with a starting " following by the string content as Unicode characters and ends in a newline character. (The lexemes file is encoded as UTF8 or Unicode, so most Unicode characters are represented directly as a single character). If a " is found *in* the string, it is an escape, and is followed by a hex character code of the actual character in the string (typically newlines, double-quotes inside the string, or line-break type Unicode characters). Comments are encoded as string containing the comment start/end characters as extracted by the DMS lexer.

## Compression Format

Each of the entries above is encoded specially to minimize the number of characters in the lexemes file.

### Lexeme Number Encoding

The lexeme number is code in radix 36, using 0-9 and a-z as digits. The number 0, normally meaning an EOF lexeme, is used instead to represent comment tokens.

### Line Number Encoding

Lexeme start and end line numbers can be encoded as unsigned decimal numbers. However, because line numbers tend to change in very regular, incremental ways, a lexeme line number can also be a single punctation character. "=" means "the same line number as the last line number". At the beginning of the file, this means "line number 1". Punctuation characters !"... (codes #21-#2F) indicate "the last line number incremented by 1-15", corresponding to the punctuation character code minus #20. Because line numbers tend to change by 0, 1 or 2 across lexemes, the punctation characters = ! and " for line number encoding is very common and one rarely sees an actual decimal line number in this field. When one does see a decimal line number, it will be seperated by spaces only if needed from other numbers.

### Column Number Encoding

Lexeme start and end column numbers can also be encoded as decimal unsigned numbers. Column numbers, however, tend to be small and to increment in very regular ways, and so can be encoded using "=" to mean, "the same column number as last seen" (which is zero if the line number has just changed), or the characters #41-#7E (A-Z, a-z, and some punctuation). If the line number has just changed, then the encoded value is the new column number. If the line number is the same as the last line number, then this value is interpreted as an increment to the last column number. In typical source code, the letters A-H are very commonly found in the column field.

### Special Case Optimizations

A number of useful special cases provide another 25-50% average size reduction:

- Where `===A` might occur (1 character lexeme), emit just `:`
- Where `===B` might occur, emit `;`
- Where `===<letter>` might occur, emit `^<letter>`
- Where `===<number>` might occur, emit `^<number>`
- Where `=<letter>=A` might occur, emit `< <letter>`
- Where `=<number>=A` might occur, emit `< <number>`
- Where `=<letter>=B` might occur, emit `> <letter>`
- Where `=<number>=B` might occur, emit `> <number>`
- Where `=<letter>=<letter>` might occur, emit `[<letter><letter>`
- Where `=<number>=<letter>` might occur, emit `[<number><letter>`
- Where `=<letter>=<number>` might occur, emit `[<letter><number>`
- Where `!<letter>=<letter>` might occur, emit `]<letter><letter>`
- Where `!<number>=<letter>` might occur, emit `]<number><letter>`
- Where `!<letter>=<number>` might occur, emit `]<letter><number>`
- Where `==` might occur, emit `@`
- Where `=A` might occur, emit `|`
- Where `=B` might occur, emit `_`

## Grammar Specification

A grammar for the lexeme information is:

```ebnf
file = DOMAIN NEWLINE FILESOURCEINFORMATION NEWLINE ENCODING NEWLINE lexeme* ;

lexeme = type RADIX36NUMBER position content NEWLINE ;

type = [A-O] ;                              -- 16 extra codes associated with token
                                            -- A U M R W D ?

RADIX36NUMBER = [0-9a-z]+ ;

position = ":"                              -- starts in same line, same column; 1 character wide token
         | ";"                              -- starts in same line, same column; 2 character wide token
         | "^" column                       -- starts in same line, same column; ends in specified column
         | "<" column                       -- starts in same line, specifed column; 1 character wide token
         | ">" column                       -- starts in same line, specifed column; 2 character wide token
         | "[" column column                -- starts in same line, specified column, with specified width
         | "]" column column                -- starts in next line, specified absolute column, with specified width
         | start_position end_position      -- starts in specified line, ends in specified line
         ;

start_position = NUMBER column              -- absolute line number
               | "=" column                 -- same line number as last
               | punctuation column         -- line number plus punctuation index
               | encoded_position
               ;

end_position = NUMBER column                -- absolute line number
             | "=" column                   -- same line number as last
             | punctuation column           -- line number plus punctuation index
             | encoded_position
             ;

column = NUMBER                             -- absolute column number
       | [ A-Z a-z ]                        -- last column number plus radix52 digit + 1
       | "="                                -- same as last column number
       ;

encoded_position = "@"                      -- same line/column as last
                 | "|"                      -- same line, column + 1
                 | "_"                      -- same line, column + 2
                 ;

punctuation = [ \! \" \# \$ \% \& \' \( \) \* \+ \, \- \. \/ ] ;  -- index establishes line increment

content =                                   -- none
        | "\"" STRINGCONTENT
        | " " NUMBER
        | "+" NUMBER
        | "-" NUMBER
        | <SIGN>? FLOAT
        | "~t"
        | "~f"
        ;
```

## Sample Output

Here is a sample of lexemes produced. Note the common occurrence of "==", meaning "same line number, same column number"; this occurs because most lexemes start in the place where the last lexeme finished.

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
