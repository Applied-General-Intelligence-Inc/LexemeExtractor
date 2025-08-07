namespace LexemeExtractor.Models;

/// <summary>
/// Represents a single lexeme entry from a .lexemes file
/// </summary>
public record Lexeme
{
    /// <summary>
    /// Complete specification unknown types are [A-O]
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Base36 number string as it appears in the lexeme file
    /// </summary>
    public string NumberString { get; init; } = string.Empty;

    /// <summary>
    /// Numeric identifier for the lexeme (converted from Base36)
    /// </summary>
    public long Number => ParseBase36(NumberString);
    
    /// <summary>
    /// Position information in the source file
    /// </summary>
    public Position Position { get; init; } = new(1, 1);

    /// <summary>
    /// Content of the lexeme (string, number, boolean, or empty)
    /// </summary>
    public LexemeContent Content { get; init; } = LexemeContent.Empty;

    /// <summary>
    /// Reference to the lexeme name definition (if available)
    /// </summary>
    public LexemeNameDefinition? NameDefinition { get; init; }

    /// <summary>
    /// Creates a new lexeme
    /// </summary>
    public Lexeme(string type, string numberString, Position position, LexemeContent content)
    {
        Type = type;
        NumberString = numberString;
        Position = position;
        Content = content;
    }

    /// <summary>
    /// Default constructor for record initialization
    /// </summary>
    public Lexeme() { }

    /// <summary>
    /// Returns a human-readable string representation
    /// </summary>
    public override string ToString()
    {
        var contentStr = Content.ToString();
        var contentDisplay = string.IsNullOrEmpty(contentStr) ? "(empty)" : contentStr;
        var nameDisplay = NameDefinition?.Name ?? GetTypeName();
        return $"Lexeme #{Number}: {nameDisplay} (Type: {Type})\n" +
               $"  Position: {Position}\n" +
               $"  Content: {contentDisplay}";
    }

    /// <summary>
    /// Gets a human-readable type name using loaded type mappings.
    /// Returns the raw type if no mapping is available.
    /// </summary>
    public string GetTypeName()
    {
        return TypeMappings.TryGetValue(Type, out var friendlyName) ? friendlyName : Type;
    }

    /// <summary>
    /// Returns true if this lexeme represents comments (type "0" confirmed from examples).
    /// Note: Other trivia types are unknown - complete lexeme type specification unavailable.
    /// </summary>
    public bool IsTrivia => Type == "0";

    /// <summary>
    /// Returns true if this lexeme has content
    /// </summary>
    public bool HasContent => Content != LexemeContent.Empty;

    #region Static Type Mapping Support

    /// <summary>
    /// Static dictionary for type name mappings. Only type "0" (Comment) is confirmed.
    /// Other mappings would need to be determined from DMS lexical specifications.
    /// </summary>
    private static readonly Dictionary<string, string> TypeMappings = new()
    {
        { "0", "Comment" } // Only confirmed mapping from documentation
    };

    /// <summary>
    /// Loads type mappings from a symbol file for a specific language/lexer.
    /// TODO: Implement when symbol files become available.
    /// </summary>
    /// <param name="symbolFilePath">Path to the symbol file containing type mappings</param>
    public static void LoadTypeMappings(string symbolFilePath)
    {
        // TODO: Implement symbol file parsing
        // Expected format might be: type_id=friendly_name (one per line)
        // or JSON/XML format depending on what DMS provides
        throw new NotImplementedException("Symbol file loading not yet implemented");
    }

    /// <summary>
    /// Adds or updates a type mapping programmatically
    /// </summary>
    /// <param name="typeId">The type identifier (e.g., "2a", "2v")</param>
    /// <param name="friendlyName">The human-readable name</param>
    public static void AddTypeMapping(string typeId, string friendlyName)
    {
        TypeMappings[typeId] = friendlyName;
    }

    /// <summary>
    /// Clears all type mappings except the confirmed ones
    /// </summary>
    public static void ClearTypeMappings()
    {
        TypeMappings.Clear();
        TypeMappings["0"] = "Comment"; // Keep the confirmed mapping
    }

    #endregion

    #region Base36 Conversion Methods

    /// <summary>
    /// Parses a Base36 string to a long value
    /// </summary>
    /// <param name="value">Base36 string to parse</param>
    /// <returns>Long value</returns>
    private static long ParseBase36(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return value.ToLowerInvariant()
            .Select(c => c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'z' => c - 'a' + 10,
                _ => throw new FormatException($"Invalid Base36 character: {c}")
            })
            .Aggregate(0L, (current, digit) => current * 36 + digit);
    }



    #endregion
}
