namespace LexemeExtractor.Models;

/// <summary>
/// Represents a lexeme name definition parsed from a domain .txt file
/// </summary>
public record LexemeNameDefinition
{
    /// <summary>
    /// Name of the lexeme (without quotes)
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Numeric identifier (converted from base16)
    /// </summary>
    public long Number { get; init; }

    /// <summary>
    /// Data type (optional, without semicolon)
    /// </summary>
    public string? DataType { get; init; }

    /// <summary>
    /// Creates a new lexeme name definition
    /// </summary>
    public LexemeNameDefinition(string name, long number, string? dataType = null)
    {
        Name = name;
        Number = number;
        DataType = dataType;
    }

    /// <summary>
    /// Default constructor for record initialization
    /// </summary>
    public LexemeNameDefinition() { }

    /// <summary>
    /// Returns a human-readable string representation
    /// </summary>
    public override string ToString()
    {
        var typeDisplay = string.IsNullOrEmpty(DataType) ? "" : $" ({DataType})";
        return $"{Name} = :{Number:X}{typeDisplay}";
    }
}
