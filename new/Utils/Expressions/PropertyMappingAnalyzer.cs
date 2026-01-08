using IvoEngine.Expressions.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IvoEngine.Expressions;

/// <summary>
/// Provides methods for analyzing type properties and creating mappings between them.
/// </summary>
public static class PropertyMappingAnalyzer
{
    /// <summary>
    /// Creates a dictionary of property mappings between source and target types.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <param name="sourceType">Type of the source class</param>
    /// <param name="targetType">Type of the target class</param>
    /// <returns>Dictionary of property mappings</returns>
    public static Dictionary<string, PropertyMappingInfo> BuildPropertyMappings<TSource, TTarget>(Type sourceType, Type targetType)
    {
        var mappings = new Dictionary<string, PropertyMappingInfo>();
        
        // Get all public properties of the source type that are not static
        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !p.GetMethod?.IsStatic ?? true)
            .ToDictionary(p => p.Name);
            
        // Get all public properties of the target type that are not static and are writable
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => (!p.GetMethod?.IsStatic ?? true) && (!p.SetMethod?.IsStatic ?? true) && p.CanWrite)
            .ToDictionary(p => p.Name);
            
        // Find properties that exist in both types and have compatible data types
        foreach (var sourceProp in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProp.Key, out var targetProp))
            {
                // Check data type compatibility
                bool isCompatible = MappingHelper.IsPropertyTypeCompatible(
                    sourceProp.Value.PropertyType, 
                    targetProp.PropertyType);
                
                mappings[sourceProp.Key] = new PropertyMappingInfo
                {
                    SourceProperty = sourceProp.Value,
                    TargetProperty = targetProp,
                    IsCompatible = isCompatible
                };
            }
        }
        
        return mappings;
    }

    /// <summary>
    /// Gets a list of property names that are mappable between source and target types.
    /// </summary>
    /// <param name="mappings">Dictionary of property mappings</param>
    /// <returns>Set of mappable property names</returns>
    public static HashSet<string> GetMappablePropertyNames(Dictionary<string, PropertyMappingInfo> mappings)
    {
        return mappings.Where(x => x.Value.IsCompatible).Select(x => x.Key).ToHashSet();
    }

    /// <summary>
    /// Filters property mappings according to specified criteria.
    /// </summary>
    /// <param name="mappings">Dictionary of property mappings</param>
    /// <param name="includeOnly">List of property names to include (null for all)</param>
    /// <returns>Filtered dictionary of property mappings</returns>
    public static Dictionary<string, PropertyMappingInfo> FilterMappingExclude(Dictionary<string, PropertyMappingInfo> mappings, IEnumerable<string>? exclude = null)
    {
        if (mappings == null || !mappings.Any())
        {
            return new Dictionary<string, PropertyMappingInfo>();
        }

        var result = new Dictionary<string, PropertyMappingInfo>(mappings);
        
        
        // Apply filter to exclude certain properties
        if (exclude is not null)
        {
            var excludeSet = new HashSet<string>(exclude);
            var keysToRemove = result.Keys.Where(key => excludeSet.Contains(key)).ToList();
                
            foreach (var key in keysToRemove)
            {
                result.Remove(key);
            }
        }

        return result;
    }

    /// <summary>
    /// Filters property mappings according to specified criteria.
    /// </summary>
    /// <param name="mappings">Dictionary of property mappings</param>
    /// <param name="includeOnly">List of property names to keep</param>
    /// <returns>Filtered dictionary of property mappings</returns>
    public static Dictionary<string, PropertyMappingInfo> FilterMapping(Dictionary<string, PropertyMappingInfo> mappings, IEnumerable<string>? includeOnly = null)
    {
        if (mappings == null || !mappings.Any())
        {
            return new Dictionary<string, PropertyMappingInfo>();
        }

        var result = new Dictionary<string, PropertyMappingInfo>(mappings);

        // Apply filter to include only certain properties
        if (includeOnly is not null)
        {
            var includeSet = new HashSet<string>(includeOnly);
            var keysToRemove = result.Keys.Where(key => !includeSet.Contains(key)).ToList();

            foreach (var key in keysToRemove)
            {
                result.Remove(key);
            }
        }

        return result;
    }
}
