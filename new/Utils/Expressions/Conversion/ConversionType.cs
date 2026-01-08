namespace IvoEngine.Expressions.Conversion;

/// <summary>
/// Represents possible types of conversions between C# types
/// </summary>
public enum ConversionType
{
    /// <summary>
    /// Conversion is not possible
    /// </summary>
    NotPossible,

    /// <summary>
    /// Implicit conversion (always safe)
    /// </summary>
    Implicit,

    /// <summary>
    /// Explicit conversion (may potentially fail at runtime)
    /// </summary>
    Explicit
}
