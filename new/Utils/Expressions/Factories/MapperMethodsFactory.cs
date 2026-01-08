using IvoEngine.Expressions.ExpressionUtils;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Reflection;
using IvoEngine.Expressions.Helpers;
using System;


namespace IvoEngine.Expressions.Factories;

/// <summary>
/// Provides standard methods for entity mapping.
/// </summary>
public static class MapperMethodsFactory
{
    /// <summary>
    /// Creates a function for mapping between source and target types.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <param name="sourceType">Source class type</param>
    /// <param name="targetType">Target class type</param>
    /// <param name="propertyMappings">Property mappings</param>
    /// <returns>Mapping function</returns>
    public static Func<TSource, TTarget> CreateMapFunctionOld<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        Dictionary<string, PropertyMappingInfo> propertyMappings)
    {
        var source = Expression.Parameter(sourceType, "source");
        var target = Expression.Variable(targetType, "target");

        var expressions = new List<Expression>();

        // Create a new instance of the target type
        expressions.Add(Expression.Assign(target, Expression.New(targetType)));

        // For each mappable property, create an expression for copying the value
        foreach (var mapping in propertyMappings.Values.Where(m => m.IsCompatible))
        {
            var sourceProperty = Expression.Property(source, mapping.SourceProperty);
            var targetProperty = Expression.Property(target, mapping.TargetProperty);

            // Create an expression for value conversion if needed
            var conversionExpression = ConversionUtils.ConversionExpression(sourceProperty, mapping.TargetProperty.PropertyType);

            // Assign the converted value to the target property
            expressions.Add(Expression.Assign(targetProperty, conversionExpression));
        }

        // Return the created target object
        expressions.Add(target);

        // Create a lambda expression for the mapping function
        return Expression.Lambda<Func<TSource, TTarget>>(
            Expression.Block(new[] { target }, expressions),
            source
        ).Compile();
    }

    public static Func<TSource, TTarget> CreateMapFunction<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        Dictionary<string, PropertyMappingInfo> propertyMappings)
    {
        var source = Expression.Parameter(sourceType, "source");

        // Create member bindings
        var memberBindings = new List<MemberBinding>();

        // For each mappable property, create a binding
        foreach (var mapping in propertyMappings.Values.Where(m => m.IsCompatible))
        {
            var sourceProperty = Expression.Property(source, mapping.SourceProperty);

            // Create an expression for value conversion if needed
            var conversionExpression = ConversionUtils.ConversionExpression(sourceProperty, mapping.TargetProperty.PropertyType);

            // Add the binding to the list
            memberBindings.Add(Expression.Bind(mapping.TargetProperty, conversionExpression));
        }

        // Create an instance with initialized values
        var memberInit = Expression.MemberInit(Expression.New(targetType), memberBindings);

        // Create a lambda expression for the mapping function
        return Expression.Lambda<Func<TSource, TTarget>>(memberInit, source).Compile();
    }

    /// <summary>
    /// Creates a function for updating the target object from the source object.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <param name="sourceType">Source class type</param>
    /// <param name="targetType">Target class type</param>
    /// <param name="propertyMappings">Property mappings</param>
    /// <returns>Update function</returns>
    public static Action<TTarget, TSource> CreateUpdateAction<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        Dictionary<string, PropertyMappingInfo> propertyMappings)
    {
        var target = Expression.Parameter(targetType, "target");
        var source = Expression.Parameter(sourceType, "source");

        var expressions = new List<Expression>();

        // For each mappable property, create an expression for updating the value
        foreach (var mapping in propertyMappings.Values.Where(m => m.IsCompatible))
        {
            var sourceProperty = Expression.Property(source, mapping.SourceProperty);
            var targetProperty = Expression.Property(target, mapping.TargetProperty);

            // Create an expression for value conversion if needed
            var conversionExpression = ConversionUtils.ConversionExpression(sourceProperty, mapping.TargetProperty.PropertyType);

            // Assign the converted value to the target property
            expressions.Add(Expression.Assign(targetProperty, conversionExpression));
        }

        // Create a lambda expression for the update function
        return Expression.Lambda<Action<TTarget, TSource>>(
            Expression.Block(expressions),
            target,
            source
        ).Compile();
    }

    public static Func<TTarget, TSource, TTarget> CreateUpdateFunction<TSource, TTarget>(
    Type sourceType,
    Type targetType,
    Dictionary<string, PropertyMappingInfo> propertyMappings)
    {
        var target = Expression.Parameter(targetType, "target");
        var source = Expression.Parameter(sourceType, "source");

        // Create a variable for the new object
        var outputValue = Expression.Variable(targetType, "newInstance");


        var expressions = new List<Expression>();
        var updateExpressions = new List<Expression>();

        var isNull = Expression.Equal(source, Expression.Constant(null, sourceType));

        // If the target type is a record, use the with expression
        if (TypeHelper.IsRecord(targetType) && targetType.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is MethodInfo cloneMethod)
        {
            // Create a copy of the object using the <Clone>$ method
            var cloneCall = Expression.Call(target, cloneMethod);

            // Assign the clone to the new variable
            updateExpressions.Add(Expression.Assign(outputValue, cloneCall));
        }
        else
        {
            // Assign the variable to the target parameter
            updateExpressions.Add(Expression.Assign(outputValue, target));
        }

        // Update properties from the mapping
        foreach (var mapping in propertyMappings.Values.Where(m => m.IsCompatible))
        {
            var sourceProperty = Expression.Property(source, mapping.SourceProperty);
            var targetProperty = Expression.Property(outputValue, mapping.TargetProperty);

            // Create an expression for value conversion if needed
            var conversionExpression = ConversionUtils.ConversionExpression(sourceProperty, mapping.TargetProperty.PropertyType);

            // Assign the value to the property of the new instance
            if (mapping.TargetProperty.CanWrite)
            {
                updateExpressions.Add(Expression.Assign(targetProperty, conversionExpression));
            }
        }

        var updateBlock = Expression.Block(updateExpressions);
        // Return the new instance
        expressions.Add(Expression.IfThenElse(
            Expression.Not(isNull),
            updateBlock,
            Expression.Assign(outputValue, target))
        );

        expressions.Add(outputValue);

        // Create the delegate type
        var delegateType = typeof(Func<,,>).MakeGenericType(targetType, sourceType, targetType);

        // Create the lambda expression
        var lambda = Expression.Lambda<Func<TTarget, TSource, TTarget>>(
            Expression.Block(new[] { outputValue }, expressions),
            target,
            source
        );


        return lambda.Compile();
    }


    /// <summary>
    /// Creates a function for comparing the source and target object.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <param name="sourceType">Source class type</param>
    /// <param name="targetType">Target class type</param>
    /// <param name="propertyMappings">Property mappings</param>
    /// <returns>Comparison function</returns>
    public static Func<TSource, TTarget, bool> CreateCompareFunction<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        Dictionary<string, PropertyMappingInfo> propertyMappings)
    {
        var source = Expression.Parameter(sourceType, "source");
        var target = Expression.Parameter(targetType, "target");

        var expressions = new List<Expression>();
        var result = Expression.Variable(typeof(bool), "result");

        // The default comparison value is true (objects are equal)
        expressions.Add(Expression.Assign(result, Expression.Constant(true)));

        // For each mappable property, create an expression for comparing values
        foreach (var mapping in propertyMappings.Values.Where(m => m.IsCompatible))
        {
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
                    var sourceIsNull = Expression.Equal(sourceProperty, Expression.Constant(null));
                    var targetIsNull = Expression.Equal(targetProperty, Expression.Constant(null));

                    // If both values are null, they are equal
                    // If one value is null and the other is not, they are not equal
                    // Otherwise, compare values using Equals
                    comparisonExpression = Expression.Condition(
                        Expression.OrElse(sourceIsNull, targetIsNull),
                        Expression.AndAlso(sourceIsNull, targetIsNull),
                        PropertyUtils.ComparisonExpression(sourceProperty, targetProperty)
                    );
                }
                else
                {
                    // For value types, direct comparison is sufficient
                    comparisonExpression = Expression.Equal(sourceProperty, targetProperty);
                }
            }
            else
            {
                // For different types, we must first convert the values
                var sourceIsNull = Expression.Equal(sourceProperty, Expression.Constant(null));
                var targetIsNull = Expression.Equal(targetProperty, Expression.Constant(null));

                // Convert the source value to the target type
                var convertedSource = ConversionUtils.ConversionExpression(
                    sourceProperty,
                    mapping.TargetProperty.PropertyType);

                comparisonExpression = Expression.Condition(
                        Expression.OrElse(sourceIsNull, targetIsNull),
                        Expression.AndAlso(sourceIsNull, targetIsNull),
                        PropertyUtils.ComparisonExpression(convertedSource, targetProperty)
                    );
            }

            expressions.Add(comparisonExpression);
        }


        if (expressions.Any())
        {
            // Combine all comparisons using logical AND
            var combinedResult = expressions.Aggregate((current, next) => Expression.AndAlso(current, next));

            var block = Expression.Block(new[] { result }, Expression.Assign(result, combinedResult), result);

            var lambda = Expression.Lambda<Func<TSource, TTarget, bool>>(block, source, target);

            return lambda.Compile();
        }
        else
        {
            // If there are no properties to compare, use reference equality

            if (sourceType == targetType)
            {

                var equalResult = Expression.Equal(source, target);

                var block = Expression.Block(new[] { result }, Expression.Assign(result, equalResult), result);

                var lambda = Expression.Lambda<Func<TSource, TTarget, bool>>(block, source, target);

                return lambda.Compile();
            }
            else
            {
                var convertedSource = ConversionUtils.ConversionExpression(source, targetType);

                var equalResult = Expression.Equal(convertedSource, target);

                var block = Expression.Block(new[] { result }, Expression.Assign(result, equalResult), result);

                var lambda = Expression.Lambda<Func<TSource, TTarget, bool>>(block, source, target);

                return lambda.Compile();
            }
        }
    }
}

/// <summary>
/// Class representing bidirectional mapping
/// </summary>
/// <typeparam name="TLeft">First type</typeparam>
/// <typeparam name="TRight">Second type</typeparam>
public class BiDirectionalMapper<TLeft, TRight>
{
    public Func<TLeft, TRight> MapLeftToRight { get; }
    public Func<TRight, TLeft> MapRightToLeft { get; }
    public Func<TRight, TLeft, TRight> UpdateRightFromLeft { get; }
    public Func<TLeft, TRight, TLeft> UpdateLeftFromRight { get; }

    public BiDirectionalMapper(
        Func<TLeft, TRight> mapLeftToRight,
        Func<TRight, TLeft> mapRightToLeft,
        Func<TRight, TLeft, TRight> updateRightFromLeft,
        Func<TLeft, TRight, TLeft> updateLeftFromRight)
    {
        MapLeftToRight = mapLeftToRight;
        MapRightToLeft = mapRightToLeft;
        UpdateRightFromLeft = updateRightFromLeft;
        UpdateLeftFromRight = updateLeftFromRight;
    }
}