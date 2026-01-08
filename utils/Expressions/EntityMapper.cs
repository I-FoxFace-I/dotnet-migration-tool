using Utils.Expressions.Factories;
using Utils.Expressions.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.Expressions;

/// <summary>
/// Provides tools for mapping, comparing, and converting from a source type to a target type.
/// Works with properties that have the same name and compatible data type in both classes.
/// For reverse mapping (from target type to source type), use EntityMapper<TTarget, TSource>.
/// </summary>
/// <typeparam name="TSource">Source data type (e.g., Entity)</typeparam>
/// <typeparam name="TTarget">Target data type (e.g., DTO)</typeparam>
public static partial class EntityMapper<TSource, TTarget>
{
    private static readonly Type _sourceType = TypeHelper.GetTargetType<TSource>();
    private static readonly Type _targetType = TypeHelper.GetTargetType<TTarget>();
    private static readonly Dictionary<string, PropertyMappingInfo> _propertyMappings = PropertyMappingAnalyzer.BuildPropertyMappings<TSource, TTarget>(SourceType, TargetType);
    private static readonly Lazy<Func<TTarget>> _createTargetFunction = new(() => StandartMethodsFactory.CreateDefaultFunction<TTarget>(TargetType));
    private static readonly Lazy<Func<TSource, TTarget>> _mappingFunction = new(() => MapperMethodsFactory.CreateMapFunction<TSource, TTarget>(SourceType, TargetType, PropertyMappings));
    private static readonly Lazy<Action<TTarget, TSource>> _updateAction = new(() => MapperMethodsFactory.CreateUpdateAction<TSource, TTarget>(SourceType, TargetType, PropertyMappings));
    private static readonly Lazy<Func<TSource, TTarget, bool>> _compareFunction = new(() => MapperMethodsFactory.CreateCompareFunction<TSource, TTarget>(SourceType, TargetType, PropertyMappings));
    private static readonly Lazy<Func<TTarget, TSource, TTarget>> _updateFunction = new(() => MapperMethodsFactory.CreateUpdateFunction<TSource, TTarget>(SourceType, TargetType, PropertyMappings));

    /// <summary>
    /// Data type of mapping source
    /// </summary>
    private static Type SourceType => _sourceType;

    /// <summary>
    /// Data type of mapping target
    /// </summary>
    private static Type TargetType => _targetType;

    /// <summary>
    /// Property mapping between source and target type
    /// </summary>
    private static Dictionary<string, PropertyMappingInfo> PropertyMappings => _propertyMappings;

    /// <summary>
    /// List of property names that exist in both types and have compatible data types
    private static readonly HashSet<string> MappablePropertyNames = PropertyMappingAnalyzer.GetMappablePropertyNames(PropertyMappings);

    /// <summary>
    /// Function to create a new instance of the target type respecting required properties
    /// </summary>
    private static Func<TTarget> CreateTargetFunction => _createTargetFunction.Value;
    /// <summary>
    /// Function for direct mapping from source type to target type
    /// </summary>
    private static readonly Func<TSource, TTarget> MappingFunction = _mappingFunction.Value;
    /// <summary>
    /// Action for updating the target object from the source object
    /// </summary>
    private static readonly Action<TTarget, TSource> UpdateAction = _updateAction.Value;
    /// <summary>
    /// Function for updating the target object from the source object
    /// </summary>
    private static readonly Func<TTarget, TSource, TTarget> UpdateFunction = _updateFunction.Value;
    /// <summary>
    /// Function for comparing the source and target object based on mappable properties
    /// </summary>
    private static readonly Func<TSource, TTarget, bool> CompareFunction = _compareFunction.Value;


    // Dictionary of functions for updating individual properties of the target object
    private static readonly Dictionary<string, Action<TTarget, TSource>> PropertyUpdateFunctions = PropertyMethodsFactory.CreatePropertyUpdateFunctions<TSource, TTarget>(SourceType, TargetType, PropertyMappings);

    // Dictionary of functions for comparing individual properties between source and target object
    private static readonly Dictionary<string, Func<TSource, TTarget, bool>> PropertyCompareFunctions = PropertyMethodsFactory.CreatePropertyCompareFunctions<TSource, TTarget>(SourceType, TargetType, PropertyMappings);

    /// <summary>
    /// Creates a new instance of the target type with default values.
    /// </summary>
    public static TTarget Empty => CreateTargetFunction();

    /// <summary>
    /// Maps the source object to a new instance of the target type.
    /// </summary>
    /// <param name="source">Source object</param>
    /// <returns>New instance of the target type with mapped values</returns>
    public static TTarget Map(TSource source)
    {
        if (source == null)
        {
            return Empty;
        }

        return MappingFunction(source);
    }

    /// <summary>
    /// Updates the target object with values from the source object.
    /// </summary>
    /// <param name="target">Target object to be updated</param>
    /// <param name="source">Source object from which values will be taken</param>
    /// <returns>Updated target object</returns>
    public static TTarget Update(TTarget target, TSource source)
    {
        if (source is null)
        {
            return target;
        }

        if (target is null)
        {
            target = Empty;
        }

        return UpdateFunction(target, source);

        //return target;
    }

    /// <summary>
    /// Compares the source and target object based on mappable properties.
    /// </summary>
    /// <param name="source">Source object</param>
    /// <param name="target">Target object</param>
    /// <returns>True if all mappable properties are equal, otherwise False</returns>
    public static bool Compare(TSource source, TTarget target)
    {
        if (source is null && target is null)
        {
            return true;
        }

        if (source is null || target is null)
        {
            return false;
        }

        return CompareFunction(source, target);
    }

    /// <summary>
    /// Updates only selected properties of the target object from the source object.
    /// </summary>
    /// <param name="target">Target object to be updated</param>
    /// <param name="source">Source object from which values will be taken</param>
    /// <param name="properties">Names of properties to be updated</param>
    /// <returns>Updated target object</returns>
    public static TTarget UpdateProperties(TTarget target, TSource source, params string[] properties)
    {
        if (source is null || properties is null || properties.Length == 0)
        {
            return target;
        }

        if (target is null)
        {
            target = Empty;
        }

        foreach (var property in properties)
        {
            if (MappablePropertyNames.Contains(property) && PropertyUpdateFunctions.TryGetValue(property, out var updateAction))
            {
                updateAction(target, source);
            }
        }

        return target;
    }

    /// <summary>
    /// Compares only selected properties of the source and target object.
    /// </summary>
    /// <param name="source">Source object</param>
    /// <param name="target">Target object</param>
    /// <param name="properties">Names of properties to be compared</param>
    /// <returns>True if all selected properties are equal, otherwise False</returns>
    public static bool CompareProperties(TSource source, TTarget target, params string[] properties)
    {
        if (source is null && target is null)
        {
            return true;
        }

        if (source is null || target is null)
        {
            return false;
        }

        if (properties is null || properties.Length == 0)
        {
            return true;
        }

        foreach (var property in properties)
        {
            if (MappablePropertyNames.Contains(property) && PropertyCompareFunctions.TryGetValue(property, out var compareFunction))
            {
                if (!compareFunction(source, target))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Updates all properties of the target object from the source object, except for the specified properties.
    /// </summary>
    /// <param name="target">Target object to be updated</param>
    /// <param name="source">Source object from which values will be taken</param>
    /// <param name="excludeProperties">Names of properties not to be updated</param>
    /// <returns>Updated target object</returns>
    public static TTarget UpdateExclude(TTarget target, TSource source, params string[] excludeProperties)
    {
        if (source == null)
        {
            return target;
        }

        if (target == null)
        {
            target = Empty;
        }

        var excludeSet = excludeProperties?.ToHashSet() ?? new HashSet<string>();

        foreach (var property in MappablePropertyNames)
        {
            if (!excludeSet.Contains(property) && PropertyUpdateFunctions.TryGetValue(property, out var updateAction))
            {
                updateAction(target, source);
            }
        }

        return target;
    }

    /// <summary>
    /// Compares all properties of the source and target object, except for the specified properties.
    /// </summary>
    /// <param name="source">Source object</param>
    /// <param name="target">Target object</param>
    /// <param name="excludeProperties">Names of properties not to be compared</param>
    /// <returns>True if all non-excluded properties are equal, otherwise False</returns>
    public static bool CompareExclude(TSource source, TTarget target, params string[] excludeProperties)
    {
        if (source == null && target == null)
        {
            return true;
        }

        if (source == null || target == null)
        {
            return false;
        }

        var excludeSet = excludeProperties?.ToHashSet() ?? new HashSet<string>();

        foreach (var property in MappablePropertyNames)
        {
            if (!excludeSet.Contains(property) && PropertyCompareFunctions.TryGetValue(property, out var compareFunction))
            {
                if (!compareFunction(source, target))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Gets all mappable properties between the source and target type.
    /// </summary>
    /// <returns>List of names of mappable properties</returns>
    public static IReadOnlyList<string> GetMappableProperties()
    {
        return MappablePropertyNames.ToList();
    }
}
