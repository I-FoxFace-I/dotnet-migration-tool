namespace IvoEngine.Expressions.Mapping
{
    public enum MappingPolicy
    {
        /// <summary>
        /// Only same types
        /// </summary>
        Strict,               // Only same types
        /// <summary>
        /// Only implicit conversions
        /// </summary>
        Implicit,
        /// <summary>
        /// Only convertible types (implicit and explicit)
        /// </summary>
        Convertible,
        /// <summary>
        /// Any (on failure, null/default is used)
        /// </summary>
        Unrestricted
    }
}
