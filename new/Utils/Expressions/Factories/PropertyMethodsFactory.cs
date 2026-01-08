using IvoEngine.Expressions.ExpressionUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace IvoEngine.Expressions.Factories;

/// <summary>
/// Provides methods for creating functions working with individual properties during entity mapping.
/// </summary>
public static class PropertyMethodsFactory
{
    /// <summary>
    /// Creates a dictionary of functions for updating individual properties.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <param name="sourceType">Source class type</param>
    /// <param name="targetType">Target class type</param>
    /// <param name="propertyMappings">Property mappings</param>
    /// <returns>Dictionary of functions for updating properties</returns>
    public static Dictionary<string, Action<TTarget, TSource>> CreatePropertyUpdateFunctions<TSource, TTarget>(
        Type sourceType, 
        Type targetType,
        Dictionary<string, PropertyMappingInfo> propertyMappings)
    {
        var updateFunctions = new Dictionary<string, Action<TTarget, TSource>>();
        
        foreach (var entry in propertyMappings)
        {
            var mapping = entry.Value;
            
            // Skip incompatible properties
            if (!mapping.IsCompatible)
            {
                continue;
            }
            
            var target = Expression.Parameter(targetType, "target");
            var source = Expression.Parameter(sourceType, "source");
            
            var sourceProperty = Expression.Property(source, mapping.SourceProperty);
            var targetProperty = Expression.Property(target, mapping.TargetProperty);

            // Create an expression for value conversion if needed
            var conversionExpression = ConversionUtils.ConversionExpression(
                sourceProperty, 
                mapping.TargetProperty.PropertyType);
            
            // Assign the converted value to the target property
            var assignExpression = Expression.Assign(targetProperty, conversionExpression);
            
            // Create a lambda expression for the update function of a specific property
            var updateFunction = Expression.Lambda<Action<TTarget, TSource>>(assignExpression,target,source).Compile();
            
            updateFunctions.Add(entry.Key,updateFunction);
        }
        
        return updateFunctions;
    }
    
    /// <summary>
    /// Creates a dictionary of functions for comparing individual properties.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <param name="sourceType">Source class type</param>
    /// <param name="targetType">Target class type</param>
    /// <param name="propertyMappings">Property mappings</param>
    /// <returns>Dictionary of functions for comparing properties</returns>
    public static Dictionary<string, Func<TSource, TTarget, bool>> CreatePropertyCompareFunctions<TSource, TTarget>(
        Type sourceType, 
        Type targetType,
        Dictionary<string, PropertyMappingInfo> propertyMappings)
    {
        var compareFunctions = new Dictionary<string, Func<TSource, TTarget, bool>>();
        
        foreach (var entry in propertyMappings)
        {
            var mapping = entry.Value;
            
            // Skip incompatible properties
            if (!mapping.IsCompatible)
            {
                continue;
            }
            
            var source = Expression.Parameter(sourceType, "source");
            var target = Expression.Parameter(targetType, "target");
            
            var sourceProperty = Expression.Property(source, mapping.SourceProperty);
            var targetProperty = Expression.Property(target, mapping.TargetProperty);
            
            // Create an expression for property comparison
            Expression comparisonExpression;
            
            // If types are the same, we can use direct value comparison
            if (mapping.SourceProperty.PropertyType == mapping.TargetProperty.PropertyType)
            {
                // For reference types, we must handle null values
                if (!mapping.SourceProperty.PropertyType.IsValueType)
                {
                    // Null value check
                    var propertyType = mapping.TargetProperty.PropertyType;
                    var sourceIsNull = Expression.Equal(sourceProperty, Expression.Constant(null));
                    var targetIsNull = Expression.Equal(targetProperty, Expression.Constant(null));

                    var valueCompareExpression = PropertyUtils.ComparisonExpression(sourceProperty, targetProperty);

                    var compareMethod = typeof(EntityManager<>).MakeGenericType(propertyType)!.GetMethod("Compare")!;

                    // If both values are null, they are equal
                    // If one value is null and the other is not, they are not equal
                    // Otherwise, compare values using Equals
                    comparisonExpression = Expression.Condition(
                        Expression.OrElse(sourceIsNull, targetIsNull),
                        Expression.AndAlso(sourceIsNull, targetIsNull),
                        valueCompareExpression
                    );
                }
                else
                {
                    // For value types, direct comparison is sufficient
                    comparisonExpression = PropertyUtils.ComparisonExpression(sourceProperty, targetProperty);
                }
            }
            else
            {
                // For different types, we must first convert the values
                var propertyType = mapping.TargetProperty.PropertyType;
                var sourceIsNull = Expression.Equal(sourceProperty, Expression.Constant(null));
                var targetIsNull = Expression.Equal(targetProperty, Expression.Constant(null));
                
                // Convert the source value to the target type
                var convertedSource = ConversionUtils.ConversionExpression(sourceProperty, propertyType);

                var valueCompareExpression = PropertyUtils.ComparisonExpression(sourceProperty, targetProperty);

                // Compare the converted value with the target
                comparisonExpression = Expression.Condition(
                    Expression.OrElse(sourceIsNull, targetIsNull),
                    Expression.AndAlso(sourceIsNull, targetIsNull),
                    valueCompareExpression
                );
            }
            
            // Create a lambda expression for the comparison function of a specific property
            var compareFunction = Expression.Lambda<Func<TSource, TTarget, bool>>(comparisonExpression, source, target).Compile();
            
            compareFunctions.Add(entry.Key, compareFunction);
        }
        
        return compareFunctions;
    }
}
