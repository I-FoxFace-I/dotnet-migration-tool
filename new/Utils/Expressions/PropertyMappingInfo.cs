using System.Reflection;

namespace IvoEngine.Expressions;

/// <summary>
/// Class holding information about property mapping between source and target types.
/// </summary>
public class PropertyMappingInfo
{
    /// <summary>
    /// Reference to the source property.
    /// </summary>
    public PropertyInfo SourceProperty { get; set; }
    
    /// <summary>
    /// Reference to the target property.
    /// </summary>
    public PropertyInfo TargetProperty { get; set; }
    
    /// <summary>
    /// Determines whether the property data types are compatible for mapping.
    /// </summary>
    public bool IsCompatible { get; set; }
}
