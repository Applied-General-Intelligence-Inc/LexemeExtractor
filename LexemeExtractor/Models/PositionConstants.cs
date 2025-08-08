namespace LexemeExtractor.Models;

/// <summary>
/// Constants for consistent initial position values across all position-related classes
/// </summary>
public static class PositionConstants
{
    /// <summary>
    /// Initial start line number for new files and position contexts
    /// </summary>
    public const int InitialStartLine = 0;

    /// <summary>
    /// Initial start column number for new lines and position contexts
    /// </summary>
    public const int InitialStartColumn = 1;

    /// <summary>
    /// Initial end line number for new files and position contexts
    /// </summary>
    public const int InitialEndLine = 0;

    /// <summary>
    /// Initial end column number for new lines and position contexts
    /// </summary>
    public const int InitialEndColumn = 1;
}
