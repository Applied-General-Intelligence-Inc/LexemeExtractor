namespace LexemeExtractor.Models;

/// <summary>
/// Represents the header information of a .lexemes file
/// </summary>
public record FileHeader
{
    /// <summary>
    /// Domain information (e.g., "Java~~Java1_5")
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// Original filename that was processed
    /// </summary>
    public string Filename { get; init; } = string.Empty;

    /// <summary>
    /// Encoding information
    /// </summary>
    public string Encoding { get; init; } = string.Empty;

    /// <summary>
    /// Creates a new file header
    /// </summary>
    public FileHeader(string domain, string filename, string encoding)
    {
        Domain = domain;
        Filename = filename;
        Encoding = encoding;
    }

    /// <summary>
    /// Default constructor for record initialization
    /// </summary>
    public FileHeader() { }

    /// <summary>
    /// Returns a human-readable string representation
    /// </summary>
    public override string ToString()
    {
        return $"File: {Filename} ({Domain})";
    }

    /// <summary>
    /// Parses domain information to extract language and version
    /// </summary>
    public (string Language, string? Version) ParseDomain()
    {
        var parts = Domain.Split("~~", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            1 => (parts[0], null),
            2 => (parts[0], parts[1]),
            _ => (Domain, null)
        };
    }
}
