using System.Text.Json.Serialization;

namespace LexemeExtractor.Models;

/// <summary>
/// Base class for lexeme content variants
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StringContent), "string")]
[JsonDerivedType(typeof(NumberContent), "number")]
[JsonDerivedType(typeof(BooleanContent), "boolean")]
[JsonDerivedType(typeof(EmptyContent), "empty")]
public abstract record LexemeContent
{
    /// <summary>
    /// Empty content singleton
    /// </summary>
    public static readonly EmptyContent Empty = new();

    /// <summary>
    /// Returns the raw value as an object
    /// </summary>
    public abstract object? Value { get; }

    /// <summary>
    /// Returns a human-readable string representation
    /// </summary>
    public abstract override string ToString();
}

/// <summary>
/// String content variant
/// </summary>
public record StringContent(string StringValue) : LexemeContent
{
    public override object Value => StringValue;
    public override string ToString() => $"\"{StringValue}\"";
}

/// <summary>
/// Numeric content variant
/// </summary>
public record NumberContent(long NumberValue) : LexemeContent
{
    public override object Value => NumberValue;
    public override string ToString() => NumberValue.ToString();
}

/// <summary>
/// Boolean content variant
/// </summary>
public record BooleanContent(bool BooleanValue) : LexemeContent
{
    public override object Value => BooleanValue;
    public override string ToString() => BooleanValue ? "true" : "false";
}

/// <summary>
/// Empty content variant
/// </summary>
public record EmptyContent : LexemeContent
{
    public override object? Value => null;
    public override string ToString() => "";
}

/// <summary>
/// Factory methods for creating lexeme content
/// </summary>
public static class LexemeContentFactory
{
    /// <summary>
    /// Creates string content
    /// </summary>
    public static StringContent String(string value) => new(value);

    /// <summary>
    /// Creates number content
    /// </summary>
    public static NumberContent Number(long value) => new(value);

    /// <summary>
    /// Creates boolean content
    /// </summary>
    public static BooleanContent Boolean(bool value) => new(value);

    /// <summary>
    /// Creates empty content
    /// </summary>
    public static EmptyContent Empty() => LexemeContent.Empty;
}
