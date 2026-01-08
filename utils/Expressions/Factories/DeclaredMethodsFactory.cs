using Utils.Expressions.Helpers;
using Utils.Expressions.ExpressionUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Utils.Expressions.Factories;

/// <summary>
/// Provides methods for working with declared properties of entities in EntityManagerNew.
/// Declared properties are those defined directly in the class (not inherited).
/// </summary>
public static class DeclaredMethodsFactory
{
    /// <summary>
    /// Creates a function for comparing two instances of a data type, considering only declared properties.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Function for comparing instances based only on declared properties</returns>
    public static Func<T, T, bool> CreateCompareDeclaredFunction<T>(Type type)
    {
        // Kontrola, zda typ implementuje vlastní metodu Equals
        if (type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.Name == "Equals").FirstOrDefault() is MethodInfo methodInfo)
        {
            return new Func<T, T, bool>((x, y) => x?.Equals(y) ?? y?.Equals(x) ?? true);
        }

        var lhs = Expression.Parameter(type, "lhsItem");
        var rhs = Expression.Parameter(type, "rhsItem");

        var expressions = new List<Expression>();
        // Get all properties of the type
        var properties = type.GetProperties();
        // Get only declared properties (excluding inherited)
        var declaredProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        if (type.IsClass)
        {
            // For each declared property, create a comparison expression
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? false)))
            {
                var obj1Prop = Expression.Property(lhs, prop);
                var obj2Prop = Expression.Property(rhs, prop);

                // Create an expression for comparing property values
                var propComparison = PropertyUtils.ComparisonExpression(obj1Prop, obj2Prop);

                expressions.Add(propComparison);
            }
        }

        var result = Expression.Variable(typeof(bool), "result");

        if (expressions.Any())
        {
            // Combine all comparisons using logical AND
            var combinedResult = expressions.Aggregate((current, next) => Expression.AndAlso(current, next));

            var block = Expression.Block(new[] { result }, Expression.Assign(result, combinedResult), result);

            var lambda = Expression.Lambda<Func<T, T, bool>>(block, lhs, rhs);

            return lambda.Compile();
        }
        else
        {
            // If there are no properties to compare, use reference equality
            var equalResult = Expression.Equal(lhs, rhs);

            var block = Expression.Block(new[] { result }, Expression.Assign(result, equalResult), result);

            var lambda = Expression.Lambda<Func<T, T, bool>>(block, lhs, rhs);

            return lambda.Compile();
        }
    }

    /// <summary>
    /// Creates a function for updating instances of a data type, considering only declared properties.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Function for updating instances based only on declared properties</returns>
    public static Action<T, T> CreateUpdateDeclaredFunction<T>(Type type)
    {
        var origin = Expression.Parameter(type, "originItem");
        var source = Expression.Parameter(type, "updateSource");

        var expressions = new List<Expression>();

        // For each declared property, create an update expression
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true)))
        {
            var targetProp = Expression.Property(origin, prop);
            var sourceProp = Expression.Property(source, prop);

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
                expressions.Add(PropertyUtils.UpdateExpression(targetProp, sourceProp, TypeHelper.IsMarkedAsNullable(prop), defaultPropertyValue));
            }
        }

        var lambda = Expression.Lambda<Action<T, T>>(Expression.Block(expressions), origin, source);

        return lambda.Compile();
    }
}
