namespace LexemeExtractor.Models;

/// <summary>
/// Constants for consistent initial position values across all position-related classes
/// </summary>
public static class PositionConstants
{
    /// <summary>
    /// Initial line number for new files and position contexts (1-based)
    /// </summary>
    public const int InitialLineNumber = 1;
    
    /// <summary>
    /// Initial column number for new lines and position contexts (1-based)
    /// </summary>
    public const int InitialColumnNumber = 1;
}
