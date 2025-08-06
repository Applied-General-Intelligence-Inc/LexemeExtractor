namespace LexemeExtractor.Parsing;

/// <summary>
/// Helper class for parsing complex position patterns
/// </summary>
internal class PositionPatternParser(string pattern, bool lineChanged = false, int lastColumn = 1)
{
    private readonly string _pattern = pattern;
    private readonly bool _lineChanged = lineChanged;
    private readonly int _lastColumn = lastColumn;
    private int _position = 0;

    public bool HasMore() => _position < _pattern.Length;

    public char PeekChar() => HasMore() ? _pattern[_position] : '\0';

    public void ConsumeChar(char expected)
    {
        if (!HasMore() || _pattern[_position] != expected)
            throw new FormatException($"Expected '{expected}' at position {_position} in pattern '{_pattern}'");
        _position++;
    }

    public int ParseNumber()
    {
        if (!HasMore() || !char.IsDigit(_pattern[_position]))
            throw new FormatException($"Expected number at position {_position} in pattern '{_pattern}'");

        var start = _position;
        while (HasMore() && char.IsDigit(_pattern[_position]))
            _position++;

        return int.Parse(_pattern[start.._position]);
    }

    public int ParseColumn()
    {
        if (!HasMore())
            throw new FormatException($"Expected column at position {_position} in pattern '{_pattern}'");

        return _pattern[_position] switch
        {
            '=' => (_position++, _lastColumn).Item2, // Same as last column
            var c when c is >= '\x41' and <= '\x7E' => ParseCharacterColumn(c),
            var c when char.IsDigit(c) => ParseNumber(),
            _ => throw new FormatException($"Invalid column character at position {_position} in pattern '{_pattern}'")
        };
    }

    private int ParseCharacterColumn(char c)
    {
        _position++;
        var encodedValue = c - 0x41 + 1;

        // According to spec: if line changed, this is absolute column number
        // If same line, this is increment to last column number
        return _lineChanged ? encodedValue : _lastColumn + encodedValue;
    }

    public int ParseColumnOrNumber() =>
        char.IsDigit(PeekChar()) ? ParseNumber() : ParseColumn();
}
