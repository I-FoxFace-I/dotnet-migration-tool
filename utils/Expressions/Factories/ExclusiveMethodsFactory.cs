using Utils.Expressions.Helpers;
using Utils.Expressions.ExpressionUtils;
using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Utils.Expressions.Factories;

/// <summary>
/// Provides methods for working with excluded properties of entities in EntityManagerNew.
/// Allows selective exclusion of certain properties when cloning, comparing, or updating entities.
/// </summary>
public static class ExclusiveMethodsFactory
{
    /// <summary>
    /// Creates a function for comparing two instances of a data type, excluding specified properties.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Function for comparing instances with the possibility to exclude properties</returns>
    public static Func<T, T, List<string>, bool> CreateExclusiveCompareFunction<T>(Type type)
    {
        var lhsItem = Expression.Parameter(type, "lhsItem");
        var rhsItem = Expression.Parameter(type, "rhsItem");
        var ignoredProperties = Expression.Parameter(typeof(List<string>), "ignoredProperties");

        var expressions = new List<Expression>();
        // Get the Contains method from Enumerable to check if the property is in the excluded list
        var containsMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "Contains").FirstOrDefault()!;
        var constructedContains = containsMethod.MakeGenericMethod(typeof(string));

        // For each property, create a comparison expression that checks if it is not excluded
        foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true)))
        {
            var lhsProperty = Expression.Property(lhsItem, prop);
            var rhsProperty = Expression.Property(rhsItem, prop);

            // Check if the property is in the excluded list
            var isExcluded = Expression.Call(null, constructedContains, ignoredProperties, Expression.Constant(prop.Name));

            // If the property is excluded, consider it equal, otherwise perform actual comparison
            var propComparison = Expression.Or(Expression.IsTrue(isExcluded), PropertyUtils.ComparisonExpression(lhsProperty, rhsProperty));

            expressions.Add(Expression.IsTrue(propComparison));
        }

        var result = Expression.Variable(typeof(bool), "result");

        if (expressions.Any())
        {
            // Combine all comparisons using logical AND
            var combinedResult = expressions.Aggregate((current, next) => Expression.AndAlso(current, next));

            var block = Expression.Block(new[] { result }, Expression.Assign(result, combinedResult), result);

            var lambda = Expression.Lambda<Func<T, T, List<string>, bool>>(block, lhsItem, rhsItem, ignoredProperties);

            return lambda.Compile();
        }
        else
        {
            // If there are no properties to compare, use reference equality
            var equalResult = Expression.Equal(lhsItem, rhsItem);

            var block = Expression.Block(new[] { result }, Expression.Assign(result, equalResult), result);

            var lambda = Expression.Lambda<Func<T, T, List<string>, bool>>(block, lhsItem, rhsItem, ignoredProperties);

            return lambda.Compile();
        }
    }

    /// <summary>
    /// Creates a function for cloning instances of a data type, excluding specified properties.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Function for cloning instances with the possibility to exclude properties</returns>
    public static Func<T, List<string>, T> CreateCloneExclusiveFunction<T>(Type type)
    {
        var original = Expression.Parameter(type, "original");
        var clone = Expression.Variable(type, "clone");
        var ignoredProperties = Expression.Parameter(typeof(List<string>), "ignoredProperties");

        var expressions = new List<Expression>();
        // Create a new instance of the type
        expressions.Add(Expression.Assign(clone, Expression.New(type)));

        // Get all properties that are not static
        var properties = type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true));

        // Get the Contains method from Enumerable to check if the property is in the excluded list
        var containsMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "Contains").FirstOrDefault()!;
        var constructedContains = containsMethod.MakeGenericMethod(typeof(string));

        // For each property, create a copy expression that checks if it is not excluded
        foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true)))
        {
            if (prop.GetMethod?.IsStatic ?? false)
            {
                continue;
            }
            if (prop.SetMethod?.IsStatic ?? false)
            {
                continue;
            }
            var originalProp = Expression.Property(original, prop);
            var cloneProp = Expression.Property(clone, prop);

            // Check if the property is in the excluded list
            var isExcluded = Expression.Call(null, constructedContains, ignoredProperties, Expression.Constant(prop.Name));

            // Copy the property only if it is not in the excluded list
            var propertyClone = Expression.IfThen(Expression.IsFalse(isExcluded),
                                           Expression.Assign(cloneProp, originalProp));
            expressions.Add(propertyClone);
        }

        // Return the resulting clone
        expressions.Add(clone);

        var lambda = Expression.Lambda<Func<T, List<string>, T>>(Expression.Block(new[] { clone }, expressions), original, ignoredProperties);
        return lambda.Compile();
    }

    /// <summary>
    /// Creates a function for updating instances of a data type, excluding specified properties.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Function for updating instances with the possibility to exclude properties</returns>
    public static Action<T, T, List<string>> CreateExclusiveUpdateFunction<T>(Type type)
    {
        var originItem = Expression.Parameter(type, "originItem");
        var updateSource = Expression.Parameter(type, "updateSource");
        var ignoredProperties = Expression.Parameter(typeof(List<string>), "ignoredProperties");

        var expressions = new List<Expression>();

        // Get the Contains method from Enumerable to check if the property is in the excluded list
        var containsMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "Contains").FirstOrDefault()!;
        var constructedContains = containsMethod.MakeGenericMethod(typeof(string));

        // For each property, create an update expression that checks if it is not excluded
        foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true)))
        {
            var targetProp = Expression.Property(originItem, prop);
            var sourceProp = Expression.Property(updateSource, prop);

            // Determine the default value for the case where the source value is null
            Expression defaultPropertyValue;
            if (prop.GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute attribute)
            {
                // Use the value from the DefaultValue attribute
                defaultPropertyValue = Expression.Constant(attribute.Value);
            }
            else
            {
                if (prop.GetCustomAttribute<RequiredMemberAttribute>() is RequiredMemberAttribute requiredAttribute)
                {
                    // For required properties, use a specialized function to create the default value
                    defaultPropertyValue = DefaultValueUtils.DefaultValue(prop.PropertyType);
                }
                else
                {
                    // For other properties, use the default
                    defaultPropertyValue = Expression.Default(prop.PropertyType);
                }
            }

            // Update the property only if it is not read-only
            if (prop.CanWrite)
            {
                // For collections (except string), special handling may be needed
                if (prop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    if (prop.GetCustomAttribute<RequiredMemberAttribute>() is RequiredMemberAttribute requiredAttribute)
                    {
                        defaultPropertyValue = DefaultValueUtils.DefaultValue(prop.PropertyType);
                    }
                    else
                    {
                        defaultPropertyValue = Expression.Default(prop.PropertyType);
                    }
                }

                // Update the property only if it is not in the excluded list
                var propertyUpdate = Expression.IfThen(
                                           Expression.IsFalse(Expression.Call(null, constructedContains, ignoredProperties, Expression.Constant(prop.Name))),
                                           PropertyUtils.UpdateExpression(targetProp, sourceProp, TypeHelper.IsMarkedAsNullable(prop), defaultPropertyValue));
                expressions.Add(propertyUpdate);
            }
        }

        return Expression.Lambda<Action<T, T, List<string>>>(Expression.Block(expressions), originItem, updateSource, ignoredProperties).Compile();
    }
}