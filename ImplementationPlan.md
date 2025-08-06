# LexemeExtractor Implementation Plan

## Current Status
✅ **Front-end Complete**: Command-line argument parsing, file globbing, stdin/stdout handling  
⏳ **Parser Implementation**: Core parsing logic needs to be implemented  
⏳ **Output Formatters**: JSON, CSV, and text formatters need implementation  

## Implementation Phases

### Phase 1: Core Infrastructure Setup
**Estimated Time**: 2-3 hours

#### 1.1 Add Dependencies
- Add ANTLR4 runtime and code generator packages
- Configure ANTLR4 build integration in .csproj

#### 1.2 Create Directory Structure
```
LexemeExtractor/
├── Grammar/           # ANTLR grammar files
├── Models/           # Data models
├── Parsing/          # Parser implementation
├── Output/           # Output formatters
└── Exceptions/       # Custom exceptions
```

#### 1.3 Define Core Models
- `LexemeFile.cs` - Root container for parsed file
- `Lexeme.cs` - Individual lexeme representation
- `Position.cs` - Line/column position data
- `LexemeContent.cs` - Content variants (string, number, boolean)
- `FileHeader.cs` - Domain, filename, encoding metadata

### Phase 2: ANTLR Grammar Definition
**Estimated Time**: 3-4 hours

#### 2.1 Create Grammar File (`Grammar/LexemeFormat.g4`)
- Define file structure: header + lexemes
- Handle position encoding patterns
- Support content variants (strings, numbers, booleans)
- Generate lexer and parser classes

#### 2.2 Test Grammar
- Create sample .lexemes files for testing
- Verify grammar parses correctly
- Debug and refine grammar rules

### Phase 3: Position Decoding System
**Estimated Time**: 4-5 hours

#### 3.1 Implement `PositionDecoder.cs`
- Handle shorthand position encodings (`:`, `;`, `@`, `|`, `_`, `=`)
- Decode punctuation-based line increments
- Decode letter-based column increments
- Parse full radix36 position encodings
- Maintain state for relative position calculations

#### 3.2 Implement `EncodingHelper.cs`
- Radix36 number parsing utilities
- Column encoding/decoding functions
- Position compression/decompression algorithms

#### 3.3 Unit Tests for Position Decoding
- Test all shorthand position cases
- Test relative position calculations
- Test edge cases and error conditions

### Phase 4: Content Decoding System
**Estimated Time**: 2-3 hours

#### 4.1 Implement `ContentDecoder.cs`
- String content decoding (quoted strings)
- Numeric content parsing (positive/negative)
- Boolean content handling (`~t`, `~f`)
- Empty content handling
- Radix36 numeric content

#### 4.2 Unit Tests for Content Decoding
- Test string parsing with escapes
- Test numeric parsing edge cases
- Test boolean and empty content

### Phase 5: ANTLR Visitor Implementation
**Estimated Time**: 3-4 hours

#### 5.1 Implement `LexemeVisitor.cs`
- Extend ANTLR base visitor
- Convert parse tree nodes to domain models
- Integrate position and content decoders
- Handle parsing errors gracefully

#### 5.2 Implement `LexemeFileParser.cs`
- High-level parsing interface
- File reading and ANTLR integration
- Error handling and reporting
- Return strongly-typed `LexemeFile` objects

### Phase 6: Output Formatters
**Estimated Time**: 3-4 hours

#### 6.1 Define Output Interface
- `IOutputFormatter.cs` - Common formatting interface
- Support for both file output and stdout

#### 6.2 Implement Formatters
- `ConsoleFormatter.cs` - Human-readable text output
- `JsonFormatter.cs` - Structured JSON output
- `CsvFormatter.cs` - Tabular CSV output

#### 6.3 Integration with Program.cs
- Replace TODO placeholders with actual parsing calls
- Wire up formatters based on command-line options
- Handle parsing errors and user feedback

### Phase 7: Error Handling & Validation
**Estimated Time**: 2-3 hours

#### 7.1 Custom Exceptions
- `LexemeParseException.cs` - Parsing-specific errors
- Include position information in error messages
- Provide helpful error descriptions

#### 7.2 Input Validation
- Validate file format and structure
- Handle malformed position encodings
- Graceful handling of unexpected content

### Phase 8: Testing & Integration
**Estimated Time**: 4-5 hours

#### 8.1 Integration Tests
- End-to-end testing with real .lexemes files
- Test all output formats
- Test stdin/stdout functionality
- Test file globbing patterns

#### 8.2 Performance Testing
- Test with large .lexemes files
- Memory usage optimization
- Streaming processing for large files

#### 8.3 Error Scenario Testing
- Malformed input files
- Invalid command-line arguments
- File system errors

## Implementation Order

### Week 1: Core Foundation
1. **Day 1-2**: Phase 1 (Infrastructure) + Phase 2 (Grammar)
2. **Day 3-4**: Phase 3 (Position Decoding)
3. **Day 5**: Phase 4 (Content Decoding)

### Week 2: Integration & Polish
1. **Day 1-2**: Phase 5 (Visitor Implementation)
2. **Day 3**: Phase 6 (Output Formatters)
3. **Day 4**: Phase 7 (Error Handling)
4. **Day 5**: Phase 8 (Testing & Integration)

## Key Technical Decisions

### ANTLR Grammar Strategy
- Use visitor pattern for tree traversal (more control than listeners)
- Handle position encoding at grammar level vs. post-processing
- Support incremental parsing for large files

### Position Decoding Architecture
- Stateful decoder maintains last position for relative calculations
- Separate concerns: parsing vs. decoding vs. modeling
- Comprehensive unit testing for all encoding patterns

### Output Format Design
- Consistent interface allows easy addition of new formats
- JSON output includes full metadata for tooling integration
- Text output optimized for human readability

### Error Handling Philosophy
- Fail fast with clear error messages
- Include file position information in errors
- Graceful degradation where possible

## Success Criteria

### Functional Requirements
- ✅ Parse all documented position encoding patterns
- ✅ Handle all content types (strings, numbers, booleans, empty)
- ✅ Generate accurate JSON, CSV, and text output
- ✅ Process multiple files via glob patterns
- ✅ Support stdin/stdout for pipeline integration

### Quality Requirements
- ✅ Comprehensive unit test coverage (>90%)
- ✅ Integration tests with real .lexemes files
- ✅ Clear error messages with position information
- ✅ Performance suitable for large files (>1MB)
- ✅ Memory efficient streaming processing

### Usability Requirements
- ✅ Intuitive command-line interface
- ✅ Helpful usage messages and examples
- ✅ Consistent output formatting
- ✅ Cross-platform compatibility (.NET 9)

## Risk Mitigation

### Technical Risks
- **ANTLR Grammar Complexity**: Start with simple grammar, iterate
- **Position Decoding Edge Cases**: Comprehensive test suite
- **Performance with Large Files**: Implement streaming early

### Schedule Risks
- **Underestimated Complexity**: Buffer time in each phase
- **ANTLR Learning Curve**: Allocate extra time for grammar development
- **Integration Issues**: Plan integration testing throughout

## Next Steps

1. **Immediate**: Begin Phase 1 - set up project structure and dependencies
2. **Priority**: Focus on position decoding as it's the most complex component
3. **Validation**: Create test .lexemes files early for continuous validation
4. **Documentation**: Update README.md with usage examples as features are completed
