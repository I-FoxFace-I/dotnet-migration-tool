namespace Utils.Expressions.Mapping
{

    public enum AutoMapping
    {
        /// <summary>
        /// Skip unmapped properties
        /// </summary>
        None,
        /// <summary>
        /// Only same type
        /// </summary>
        ExactMatch,
        /// <summary>
        /// Only implicit conversions
        /// </summary>
        ImplicitOnly,
        /// <summary>
        /// Automatically map all
        /// </summary>
        NameBasedMapping,
    }
}
