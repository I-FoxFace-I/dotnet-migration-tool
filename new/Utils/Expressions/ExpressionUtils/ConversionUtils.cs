using System.Linq.Expressions;
using System.Reflection;
using IvoEngine.Expressions.Conversion;
using StringConverter = IvoEngine.Expressions.Conversion.StringConverter;
using System.ComponentModel;
using IvoEngine.Expressions.Helpers;
using IvoEngine.Expressions.StaticData;

namespace IvoEngine.Expressions.ExpressionUtils;

public static class ConversionUtils
{
    #region Public API

    /// <summary>
    /// Creates an expression for converting a value from the source type to the target type.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Expression for conversion</returns>
    public static Expression ConversionExpression(Expression sourceExpression, Type targetType)
    {
        return CreateConversionExpressionInternal(sourceExpression, targetType);
    }

    /// <summary>
    /// Creates an expression for converting a value from the source type to the target type based on conversion info.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="targetType">Target data type</param>
    /// <param name="conversionInfo">Conversion info</param>
    /// <returns>Expression for conversion</returns>
    public static Expression CreateConversionExpression(Expression sourceExpression, Type targetType, Helpers.ConversionInfo conversionInfo)
    {
        Type sourceType = sourceExpression.Type;

        // Check if types are the same (identity)
        if (conversionInfo.IsIdentity)
        {
            return sourceExpression;
        }

        // Check for null value conversion
        if (!sourceType.IsValueType)
        {
            var nullCheck = Expression.Equal(sourceExpression, Expression.Constant(null, sourceType));
            var defaultValue = Expression.Default(targetType);

            // For reference types, we must handle null value
            return Expression.Condition(
                nullCheck,
                defaultValue,
                CreateNonNullConversionExpression(sourceExpression, sourceType, targetType, conversionInfo)
            );
        }

        return CreateNonNullConversionExpression(sourceExpression, sourceType, targetType, conversionInfo);
    }

    /// <summary>
    /// Creates an expression for converting a value based on the best available conversion method.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <param name="conversionInfo">Conversion info</param>
    /// <returns>Expression for conversion</returns>
    public static Expression CreateNonNullConversionExpression(Expression sourceExpression, Type sourceType, Type targetType, Helpers.ConversionInfo conversionInfo)
    {
        // Prioritize conversion methods by reliability and performance

        // 1. Primitive type conversion
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.Primitive))
        {
            return CreatePrimitiveConversionExpression(sourceExpression, sourceType, targetType, conversionInfo.IsImplicit);
        }

        // 2. Enum type conversion
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.Enum))
        {
            return CreateEnumConversionExpression(sourceExpression, sourceType, targetType);
        }

        // 3. Conversion using operators
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.Operator))
        {
            return CreateOperatorConversionExpression(sourceExpression, sourceType, targetType);
        }

        // 4. Conversion based on inheritance or interface implementation
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.Inheritance) ||
            conversionInfo.HasMethod(Helpers.ConversionMethod.Interface))
        {
            return Expression.Convert(sourceExpression, targetType);
        }

        // 5. Collection conversion
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.Collection))
        {
            return CreateCollectionConversionExpression(sourceExpression, sourceType, targetType);
        }

        // 6. Conversion between generic types
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.Generic))
        {
            return CreateGenericTypeConversionExpression(sourceExpression, sourceType, targetType);
        }

        // 7. Conversion using TypeConverter
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.TypeConverter))
        {
            if (TryCreateTypeConverterConversion(sourceExpression, sourceType, targetType, out var typeConverterExpression))
            {
                return typeConverterExpression;
            }
        }

        // 8. Conversion using constructor
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.Constructor))
        {
            return CreateConstructorConversionExpression(sourceExpression, sourceType, targetType);
        }

        // 9. Property mapping for complex objects
        if (conversionInfo.HasMethod(Helpers.ConversionMethod.PropertyMapping))
        {
            return CreateObjectMappingExpression(sourceExpression, sourceType, targetType);
        }

        // If we have no other way, use direct conversion (may fail at runtime)
        return Expression.Convert(sourceExpression, targetType);
    }

    /// <summary>
    /// Creates an expression for primitive type conversion.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <param name="useImplicitOnly">Indicates whether to use only implicit conversions</param>
    /// <returns>Expression for conversion</returns>
    public static Expression CreatePrimitiveConversionExpression(Expression sourceExpression, Type sourceType, Type targetType, bool useImplicitOnly = false)
    {
        // For primitive types, use direct conversion or special converters
        if (useImplicitOnly || IsImplicitConversion(sourceType, targetType))
        {
            // Try to use ImplicitConverter
            string methodName = $"To{GetTypeName(targetType)}";
            var method = typeof(ImplicitConverter).GetMethod(methodName, new[] { sourceType });

            if (method != null)
            {
                return Expression.Call(null, method, sourceExpression);
            }

            return Expression.Convert(sourceExpression, targetType);
        }
        else
        {
            // Try to use ExplicitConverter for safer conversions
            string methodName = $"To{GetTypeName(targetType)}";
            var method = typeof(ExplicitConverter).GetMethod(methodName, new[] { sourceType });

            if (method != null)
            {
                return Expression.Call(null, method, sourceExpression);
            }

            // Special case for conversions from/to string
            if (sourceType == TypesInfo.StringType || targetType == TypesInfo.StringType)
            {
                if (sourceType == TypesInfo.StringType)
                {
                    methodName = $"To{GetTypeName(targetType)}";
                    var specialMethod = typeof(SpecialConverter).GetMethod(methodName, new[] { sourceType });

                    if (specialMethod != null)
                    {
                        return Expression.Call(null, specialMethod, sourceExpression);
                    }
                }
                else if (targetType == TypesInfo.StringType)
                {
                    methodName = "ToString";
                    var stringMethod = typeof(StringConverter).GetMethod(methodName, new[] { sourceType });

                    if (stringMethod != null)
                    {
                        return Expression.Call(null, stringMethod, sourceExpression);
                    }
                }
            }

            // If no suitable converter, use direct conversion
            return Expression.Convert(sourceExpression, targetType);
        }
    }

    /// <summary>
    /// Creates an expression for enum type conversion.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Expression for conversion</returns>
    public static Expression CreateEnumConversionExpression(Expression sourceExpression, Type sourceType, Type targetType)
    {
        if (sourceType.IsEnum && targetType.IsEnum)
        {
            // Same enum type
            return sourceExpression;
        }
        else if (sourceType.IsEnum)
        {
            // Enum to its underlying type or another type
            var enumUnderlyingType = Enum.GetUnderlyingType(sourceType);
            var enumValue = Expression.Convert(sourceExpression, enumUnderlyingType);

            if (enumUnderlyingType == targetType)
            {
                return enumValue;
            }
            else if (targetType == typeof(string))
            {
                // Enum to string
                var toStringMethod = sourceType.GetMethod("ToString", Type.EmptyTypes)!;
                return Expression.Call(sourceExpression, toStringMethod);
            }
            else
            {
                // Enum to another type via underlying type
                return CreatePrimitiveConversionExpression(enumValue, enumUnderlyingType, targetType);
            }
        }
        else if (targetType.IsEnum)
        {
            // Another type to enum
            var enumUnderlyingType = Enum.GetUnderlyingType(targetType);

            if (sourceType == typeof(string))
            {
                // String to enum via Enum.Parse
                var parseMethod = typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) })!;
                var parseCall = Expression.Call(
                    null,
                    parseMethod,
                    Expression.Constant(targetType),
                    sourceExpression,
                    Expression.Constant(true)
                );

                return Expression.Convert(parseCall, targetType);
            }
            else
            {
                // Convert to underlying type and then to enum
                var convertedValue = CreatePrimitiveConversionExpression(sourceExpression, sourceType, enumUnderlyingType);
                return Expression.Convert(convertedValue, targetType);
            }
        }

        return Expression.Convert(sourceExpression, targetType);
    }

    /// <summary>
    /// Creates an expression for conversion using operators.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Expression for conversion</returns>
    public static Expression CreateOperatorConversionExpression(Expression sourceExpression, Type sourceType, Type targetType)
    {
        // First, try to find an implicit operator
        var implicitOperator = FindConversionOperator(sourceType, targetType, "op_Implicit");
        if (implicitOperator != null)
        {
            return Expression.Call(null, implicitOperator, sourceExpression);
        }

        // If implicit operator not found, use explicit
        var explicitOperator = FindConversionOperator(sourceType, targetType, "op_Explicit");
        if (explicitOperator != null)
        {
            return Expression.Call(null, explicitOperator, sourceExpression);
        }

        // If neither found, use direct conversion (may fail)
        return Expression.Convert(sourceExpression, targetType);
    }

    /// <summary>
    /// Creates an expression for conversion between generic types.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Expression for conversion</returns>
    public static Expression CreateGenericTypeConversionExpression(Expression sourceExpression, Type sourceType, Type targetType)
    {
        // For generic types, we usually need to create a new instance
        if (sourceType.IsGenericType && targetType.IsGenericType &&
            sourceType.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition())
        {
            var sourceGenericArgs = sourceType.GetGenericArguments();
            var targetGenericArgs = targetType.GetGenericArguments();

            // For simple cases, direct conversion is enough
            if (targetType.IsInterface && targetType.IsAssignableFrom(sourceType))
            {
                return Expression.Convert(sourceExpression, targetType);
            }

            // For more complex cases, we can create a new instance with converted generic parameters
            // This is an advanced implementation that would require further analysis
        }

        // If we have no better way, try direct conversion
        if (targetType.IsAssignableFrom(sourceType))
        {
            return Expression.Convert(sourceExpression, targetType);
        }

        // In other cases, we can try property mapping or constructing a new instance
        return CreateObjectMappingExpression(sourceExpression, sourceType, targetType);
    }

    /// <summary>
    /// Creates an expression for conversion using constructor of the target type.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Expression for conversion</returns>
    public static Expression CreateConstructorConversionExpression(Expression sourceExpression, Type sourceType, Type targetType)
    {
        var constructor = targetType.GetConstructor(new[] { sourceType });
        if (constructor != null)
        {
            return Expression.New(constructor, sourceExpression);
        }

        // If no suitable constructor, try some other way of conversion
        return Expression.Convert(sourceExpression, targetType);
    }

    /// <summary>
    /// Creates an expression for conversion between nullable types.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Expression for conversion</returns>
    public static Expression CreateNullableConversionExpression(Expression sourceExpression, Type sourceType, Type targetType)
    {
        // Check nullable types
        bool isSourceNullable = TypeHelper.IsNullable(sourceType, out var sourceUnderlyingType);
        bool isTargetNullable = TypeHelper.IsNullable(targetType, out var targetUnderlyingType);

        // 1. Conversion from Nullable<T> to T (nullable to non-nullable)
        if (isSourceNullable && !isTargetNullable)
        {
            if (sourceUnderlyingType == targetType)
            {
                // Direct conversion from Nullable<T> to T with null check
                return Expression.Condition(
                    Expression.Equal(sourceExpression, Expression.Constant(null, sourceType)),
                    Expression.Default(targetType), // Default value if source is null
                    Expression.Convert(sourceExpression, targetType)
                );
            }

            // First convert Nullable<T> to T, then T to the target type
            var nonNullableSource = Expression.Condition(
                Expression.Equal(sourceExpression, Expression.Constant(null, sourceType)),
                Expression.Default(sourceUnderlyingType!), // Default value of underlying type
                Expression.Convert(sourceExpression, sourceUnderlyingType!)
            );

            // Recursive call for conversion from underlying type to target type
            return CreateConversionExpressionInternal(nonNullableSource, targetType);
        }

        // 2. Conversion from T to Nullable<T> (non-nullable to nullable)
        if (!isSourceNullable && isTargetNullable)
        {
            if (sourceType == targetUnderlyingType)
            {
                // Direct conversion from T to Nullable<T>
                return Expression.Convert(sourceExpression, targetType);
            }

            // First convert the source type to the target's underlying type, then to Nullable<T>
            var convertedValue = CreateConversionExpressionInternal(sourceExpression, targetUnderlyingType!);

            return Expression.Convert(convertedValue, targetType);
        }

        // 3. Conversion from Nullable<T> to Nullable<U>
        if (isSourceNullable && isTargetNullable)
        {
            if (sourceUnderlyingType! == targetUnderlyingType!)
            {
                return sourceExpression;
            }
            // Check if the source is null
            var isNull = Expression.Equal(sourceExpression, Expression.Constant(null, sourceType));

            // Conversion from Nullable<T> to T
            var nonNullableSource = Expression.Condition(
                isNull,
                Expression.Default(sourceUnderlyingType!),
                Expression.Convert(sourceExpression, sourceUnderlyingType!)
            );

            // Conversion from T to U
            var convertedValue = CreateConversionExpressionInternal(nonNullableSource, targetUnderlyingType!);

            // Final result: null if the source is null, otherwise the converted value
            return Expression.Condition(
                isNull,
                Expression.Constant(null, targetType),
                Expression.Convert(convertedValue, targetType)
            );
        }

        // If we have no special case, use standard conversion
        return CreateConversionExpressionInternal(sourceExpression, targetType);
    }

    /// <summary>
    /// Creates a function for converting a value from the source type to the target type.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <returns>Conversion function</returns>
    public static Func<TSource, TTarget> CreateConversionFunction<TSource, TTarget>()
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        // Get conversion info
        var conversionInfo = ConversionHelper.GetConversionInfo(sourceType, targetType);

        if (!conversionInfo.CanConvert)
        {
            throw new InvalidOperationException($"Cannot create conversion function from type {sourceType.Name} to type {targetType.Name}. Conversion is not possible.");
        }

        // Input parameter for conversion function
        var sourceParam = Expression.Parameter(sourceType, "source");

        // Create conversion expression based on available methods
        var conversionExpr = CreateConversionExpression(sourceParam, targetType, conversionInfo);

        // Create and compile lambda expression
        var lambda = Expression.Lambda<Func<TSource, TTarget>>(conversionExpr, sourceParam);
        return lambda.Compile();
    }

    #endregion

    #region Internal Implementation

    /// <summary>
    /// Internal implementation for creating an expression that converts a value from the source type to the target type.
    /// </summary>
    /// <param name="sourceExpression">Expression representing the source value</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Expression for conversion</returns>
    internal static Expression CreateConversionExpressionInternal(Expression sourceExpression, Type targetType)
    {
        Type sourceType = sourceExpression.Type;

        // If types are the same, no conversion is needed
        if (sourceType == targetType)
        {
            return sourceExpression;
        }

        // Check nullable types
        bool isSourceNullable = TypeHelper.IsNullable(sourceType, out var sourceUnderlyingType);
        bool isTargetNullable = TypeHelper.IsNullable(targetType, out var targetUnderlyingType);

        // 1. Conversion from Nullable<T> to T (nullable to non-nullable)
        if (isSourceNullable && !isTargetNullable)
        {
            if (sourceUnderlyingType == targetType)
            {
                // Direct conversion from Nullable<T> to T with null check
                return Expression.Condition(
                    Expression.Equal(sourceExpression, Expression.Constant(null, sourceType)),
                    Expression.Default(targetType), // Default value if source is null
                    Expression.Convert(sourceExpression, targetType)
                );
            }

            // First convert Nullable<T> to T, then T to the target type
            var nonNullableSource = Expression.Condition(
                Expression.Equal(sourceExpression, Expression.Constant(null, sourceType)),
                Expression.Default(sourceUnderlyingType!), // Default value of underlying type
                Expression.Convert(sourceExpression, sourceUnderlyingType!)
            );

            // Recursive call for conversion from underlying type to target type
            return CreateConversionExpressionInternal(nonNullableSource, targetType);
        }

        // 2. Conversion from T to Nullable<T> (non-nullable to nullable)
        if (!isSourceNullable && isTargetNullable)
        {
            if (sourceType == targetUnderlyingType)
            {
                // Direct conversion from T to Nullable<T>
                return Expression.Convert(sourceExpression, targetType);
            }

            // First convert the source type to the target's underlying type, then to Nullable<T>
            var convertedValue = CreateConversionExpressionInternal(sourceExpression, targetUnderlyingType!);

            return Expression.Convert(convertedValue, targetType);
        }

        // 3. Conversion from Nullable<T> to Nullable<U>
        if (isSourceNullable && isTargetNullable)
        {
            if (sourceUnderlyingType! == targetUnderlyingType!)
            {
                return sourceExpression;
            }
            // Check if the source is null
            var isNull = Expression.Equal(sourceExpression, Expression.Constant(null, sourceType));

            // Conversion from Nullable<T> to T
            var nonNullableSource = Expression.Condition(
                isNull,
                Expression.Default(sourceUnderlyingType!),
                Expression.Convert(sourceExpression, sourceUnderlyingType!)
            );

            // Conversion from T to U
            var convertedValue = CreateConversionExpressionInternal(nonNullableSource, targetUnderlyingType!);

            // Final result: null if the source is null, otherwise the converted value
            return Expression.Condition(
                isNull,
                Expression.Constant(null, targetType),
                Expression.Convert(convertedValue, targetType)
            );
        }

        // 4. Check if it is a collection conversion
        bool isSourceCollection = TypeHelper.IsCollectionType(sourceType);
        bool isTargetCollection = TypeHelper.IsCollectionType(targetType);

        if (isSourceCollection && isTargetCollection)
        {
            return CreateCollectionConversionExpression(sourceExpression, sourceType, targetType);
        }

        // 5. Check for implicit and explicit conversions for primitive types
        if (TryCreatePrimitiveConversion(sourceExpression, sourceType, targetType, out var primitiveConversion))
        {
            return primitiveConversion;
        }

        // 6. Check if a conversion operator exists
        if (TryCreateOperatorConversion(sourceExpression, sourceType, targetType, out var operatorConversion))
        {
            return operatorConversion;
        }

        // 7. Check if TypeConverter can be used
        if (TryCreateTypeConverterConversion(sourceExpression, sourceType, targetType, out var typeConverterConversion))
        {
            return typeConverterConversion;
        }

        // 8. For complex objects, try to map properties
        if (!IsPrimitiveOrSystemType(sourceType) && !IsPrimitiveOrSystemType(targetType))
        {
            return CreateObjectMappingExpression(sourceExpression, sourceType, targetType);
        }

        // If no other way of conversion can be found, use direct conversion
        // This may fail at runtime if types are not compatible
        try
        {
            return Expression.Convert(sourceExpression, targetType);
        }
        catch
        {
            // If even direct conversion is not possible, return the default value of the target type
            return Expression.Default(targetType);
        }
    }

    /// <summary>
    /// Creates an expression for converting between collections.
    /// </summary>
    internal static Expression CreateCollectionConversionExpression(Expression sourceExpression, Type sourceType, Type targetType)
    {
        // Get the element type of collections
        Type sourceElementType = TypeHelper.GetElementType(sourceType) ?? typeof(object);
        Type targetElementType = TypeHelper.GetElementType(targetType) ?? typeof(object);

        // Special case for dictionaries (KeyValuePair collection)
        if (TypeHelper.IsKeyValuePair(sourceElementType) && TypeHelper.IsKeyValuePair(targetElementType))
        {
            // For dictionaries, we must create a special conversion because elements are KeyValuePair
            // We could use existing methods for dictionary conversion if available
            return CreateDictionaryConversionExpression(sourceExpression, sourceType, targetType, sourceElementType, targetElementType);
        }

        // Create a variable for the result
        var resultVar = Expression.Variable(targetType, "result");
        var expressions = new List<Expression>();

        // Initialize the result variable with the default value
        expressions.Add(Expression.Assign(resultVar, DefaultValueUtils.DefaultValue(targetType)));

        // Null value check
        //var nullCheck = Expression.Equal(sourceExpression, Expression.Constant(null, sourceType));
        //expressions.Add(Expression.IfThen(nullCheck, Expression.Return(Expression.Label(), resultVar)));

        // If the element types are the same, we can use MethodUtils.UpdateCollection
        if (sourceElementType == targetElementType)
        {
            // Use MethodUtils.UpdateCollection to update the collection
            expressions.Add(MethodUtils.UpdateCollection(
                targetType,
                resultVar,
                sourceExpression,
                TypeHelper.IsNullable(targetType, out _),
                DefaultValueUtils.DefaultValue(targetType)
            ));
        }
        else
        {
            // Conversion of a collection with different element types
            // First, create a converted collection using LINQ Select

            var enumerableType = TypesInfo.EnumerableType.MakeGenericType(targetElementType);
            var convertedCollection = Expression.Variable(enumerableType, "convertedCollection");
            // Create an expression for collection conversion using Select
            var selectMethod = MethodsInfo.MakeSelect(sourceElementType, targetElementType);

            var conversionExpressions = new List<Expression>();

            // Create a lambda expression for converting individual elements
            var itemParam = Expression.Parameter(sourceElementType, "item");
            var convertItem = CreateConversionExpressionInternal(itemParam, targetElementType);

            var convertLambda = Expression.Lambda(convertItem, itemParam);

            // Assign the result of the conversion to the convertedCollection variable
            conversionExpressions.Add(Expression.Assign(convertedCollection,
                Expression.Call(null, selectMethod, sourceExpression, convertLambda)));

            // Create an expression to update the target dictionary
            var updateExpr = MethodUtils.UpdateCollection(
                targetType,
                resultVar,
                convertedCollection,
                TypeHelper.IsNullable(targetType, out _),
                DefaultValueUtils.DefaultValue(targetType)
            );

            conversionExpressions.Add(updateExpr);

            // Create a nested block with the convertedCollection variable
            var conversionBlock = Expression.Block(
                new[] { convertedCollection },
                conversionExpressions
            );

            // Add the nested block to the main list of expressions
            expressions.Add(conversionBlock);
        }

        // Return the result variable
        expressions.Add(resultVar);
        return Expression.Block(new[] { resultVar }, expressions);
    }

    /// <summary>
    /// Creates an expression for converting between dictionaries.
    /// </summary>
    internal static Expression CreateDictionaryConversionExpression(
        Expression sourceExpression,
        Type sourceType,
        Type targetType,
        Type sourceElementType,
        Type targetElementType)
    {
        // Get the key and value types for the source dictionary
        var sourceKeyType = sourceElementType.GetGenericArguments()[0];
        var sourceValueType = sourceElementType.GetGenericArguments()[1];

        // Get the key and value types for the target dictionary
        var targetKeyType = targetElementType.GetGenericArguments()[0];
        var targetValueType = targetElementType.GetGenericArguments()[1];

        // Create a variable for the result
        var resultVar = Expression.Variable(targetType, "result");
        var expressions = new List<Expression>();

        // Initialize the result variable with the default value
        expressions.Add(Expression.Assign(resultVar, DefaultValueUtils.DefaultValue(targetType)));

        // Null value check
        //var nullCheck = Expression.Equal(sourceExpression, Expression.Constant(null, sourceType));
        //expressions.Add(Expression.IfThen(nullCheck, Expression.Return(Expression.Label(), resultVar)));

        // If the key and value types are the same, we can use a simpler approach
        if (sourceKeyType == targetKeyType && sourceValueType == targetValueType)
        {
            // Use MethodUtils.UpdateCollection to update the dictionary
            expressions.Add(MethodUtils.UpdateCollection(
                targetType,
                resultVar,
                sourceExpression,
                TypeHelper.IsNullable(targetType, out _),
                DefaultValueUtils.DefaultValue(targetType)
            ));
        }
        else
        {
            // For dictionaries with different key or value types, we must create a converted dictionary
            // Use LINQ Select to convert KeyValuePair elements

            // Create a variable for the converted KeyValuePair collection
            var targetKvpType = TypesInfo.KeyValuePairType.MakeGenericType(targetKeyType, targetValueType);
            var enumerableType = TypesInfo.EnumerableType.MakeGenericType(targetKvpType);
            var convertedCollection = Expression.Variable(enumerableType, "convertedCollection");

            // Create a nested block for processing the conversion
            var conversionExpressions = new List<Expression>();

            // Create a lambda expression for converting individual key-value pairs
            var itemParam = Expression.Parameter(sourceElementType, "kvp");

            // Get the key and value from the source KeyValuePair
            var keyProperty = sourceElementType.GetProperty("Key");
            var valueProperty = sourceElementType.GetProperty("Value");
            var keyExpr = Expression.Property(itemParam, keyProperty!);
            var valueExpr = Expression.Property(itemParam, valueProperty!);

            // Convert the key and value
            var convertedKey = CreateConversionExpressionInternal(keyExpr, targetKeyType);
            var convertedValue = CreateConversionExpressionInternal(valueExpr, targetValueType);

            // Create a new KeyValuePair with the converted values
            var kvpConstructor = targetKvpType.GetConstructor(new[] { targetKeyType, targetValueType });
            var newKvp = Expression.New(kvpConstructor!, convertedKey, convertedValue);

            // Create a lambda expression
            var convertLambda = Expression.Lambda(newKvp, itemParam);

            // Create an expression for collection conversion using Select
            var selectMethod = MethodsInfo.MakeSelect(sourceElementType, targetKvpType);

            // Assign the result of the conversion to the convertedCollection variable
            conversionExpressions.Add(Expression.Assign(convertedCollection,
                Expression.Call(null, selectMethod, sourceExpression, convertLambda)));

            // Create an expression to update the target dictionary
            var updateExpr = MethodUtils.UpdateCollection(
                targetType,
                resultVar,
                convertedCollection,
                TypeHelper.IsNullable(targetType, out _),
                DefaultValueUtils.DefaultValue(targetType)
            );

            conversionExpressions.Add(updateExpr);

            // Create a nested block with the convertedCollection variable
            var conversionBlock = Expression.Block(new[] { convertedCollection }, conversionExpressions);

            // Add the nested block to the main list of expressions
            expressions.Add(conversionBlock);
        }

        // Return the result variable
        expressions.Add(resultVar);
        return Expression.Block(new[] { resultVar }, expressions);
    }

    /// <summary>
    /// Creates an expression for mapping properties between complex objects.
    /// </summary>
    internal static Expression CreateObjectMappingExpression(Expression sourceExpression, Type sourceType, Type targetType)
    {
        Expression defaultValue;
        // Create a variable for the target object
        var targetVar = Expression.Variable(targetType, "target");
        var expressions = new List<Expression>();

        // Null value check
        var isNull = Expression.Equal(sourceExpression, Expression.Constant(null, sourceType));

        if (TypeHelper.IsNullable(targetType, out _))
        {
            defaultValue = Expression.Constant(null, targetType);
        }
        else
        {
            defaultValue = DefaultValueUtils.DefaultValue(targetType);
        }

        // If the source is null, return null or default value
        if (!targetType.IsValueType)
        {
            expressions.Add(Expression.IfThen(isNull, Expression.Assign(targetVar, Expression.Constant(null, targetType))));
        }
        else
        {
            expressions.Add(Expression.IfThen(isNull, Expression.Assign(targetVar, Expression.Default(targetType))));
        }

        // Create a new instance of the target type
        if (ConstucotrHelper.HasSimpleConstructor(targetType, out var constructorInfo) && constructorInfo != null)
        {
            expressions.Add(
                Expression.IfThen(
                    Expression.Not(isNull),
                    Expression.Assign(targetVar, Expression.New(constructorInfo))
                )
            );

            // Get the properties of the source and target types
            var sourceProperties = sourceType.GetProperties()
                .Where(p => p.CanRead && (p.GetMethod?.IsPublic ?? false) && (!p.GetMethod?.IsStatic ?? false));

            var targetProperties = targetType.GetProperties()
                .Where(p => p.CanWrite && (p.SetMethod?.IsPublic ?? false) && (!p.SetMethod?.IsStatic ?? false))
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            // For each property of the source type, try to find a corresponding property in the target type
            foreach (var sourceProp in sourceProperties)
            {
                // Try to find a property in the target type by name
                if (targetProperties.TryGetValue(sourceProp.Name, out var targetProp))
                {
                    // Check if it is possible to convert between property types
                    if (ConversionHelper.IsTypeConvertible(sourceProp.PropertyType, targetProp.PropertyType))
                    {
                        // Create an expression to get the value from the source property
                        var propValue = Expression.Property(sourceExpression, sourceProp);

                        // Create an expression to convert the value to the target property type
                        var convertedValue = CreateConversionExpressionInternal(propValue, targetProp.PropertyType);

                        // Assign the converted value to the target property
                        expressions.Add(
                            Expression.IfThen(
                                Expression.Not(isNull),
                                Expression.Assign(Expression.Property(targetVar, targetProp), convertedValue)
                            )
                        );
                    }
                }
            }
        }
        else
        {
            // If a parameterless constructor is not available, we cannot create an instance
            expressions.Add(Expression.Assign(targetVar, Expression.Default(targetType)));
        }

        // Return the target object
        expressions.Add(targetVar);

        // Create and return a block of expressions
        return Expression.Block(new[] { targetVar }, expressions);
    }

    /// <summary>
    /// Tries to create an expression for primitive type conversion.
    /// </summary>
    internal static bool TryCreatePrimitiveConversion(
        Expression sourceExpression,
        Type sourceType,
        Type targetType,
        out Expression conversion)
    {
        conversion = Expression.Empty();

        // Check if both types are primitive or system types
        if (!TypeHelper.IsPrimitiveType(sourceType) || !TypeHelper.IsPrimitiveType(targetType))
        {
            return false;
        }

        // Implicit conversion
        if (IsImplicitConversion(sourceType, targetType))
        {
            string methodName = $"To{GetTypeName(targetType)}";
            var method = typeof(ImplicitConverter).GetMethod(methodName, new[] { sourceType });

            if (method is not null)
            {
                conversion = Expression.Call(null, method, sourceExpression);
                return true;
            }
        }

        // Explicit conversion
        if (IsExplicitConversion(sourceType, targetType))
        {
            string methodName = $"To{GetTypeName(targetType)}";
            var method = typeof(ExplicitConverter).GetMethod(methodName, new[] { sourceType });

            if (method is not null)
            {
                conversion = Expression.Call(null, method, sourceExpression);
                return true;
            }
        }

        // Special case for conversions from/to string
        if (sourceType == TypesInfo.StringType || targetType == TypesInfo.StringType)
        {
            string methodName;

            if (sourceType == TypesInfo.StringType)
            {
                methodName = $"To{GetTypeName(targetType)}";
                var method = typeof(SpecialConverter).GetMethod(methodName, new[] { sourceType });

                if (method is not null)
                {
                    conversion = Expression.Call(null, method, sourceExpression);
                    return true;
                }
            }
            else if (targetType == TypesInfo.StringType)
            {
                methodName = "ToString";
                var method = typeof(StringConverter).GetMethod(methodName, new[] { sourceType });

                if (method is not null)
                {
                    conversion = Expression.Call(null, method, sourceExpression);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to create an expression for conversion using operators.
    /// </summary>
    internal static bool TryCreateOperatorConversion(
        Expression sourceExpression,
        Type sourceType,
        Type targetType,
        out Expression conversion)
    {
        conversion = Expression.Empty();

        // Check if an explicit conversion operator exists
        var explicitConversionName = "op_Explicit";
        var explicitMethod = FindConversionOperator(sourceType, targetType, explicitConversionName);

        if (explicitMethod is not null)
        {
            conversion = Expression.Call(null, explicitMethod, sourceExpression);
            return true;
        }

        // Check if an implicit conversion operator exists
        var implicitConversionName = "op_Implicit";
        var implicitMethod = FindConversionOperator(sourceType, targetType, implicitConversionName);

        if (implicitMethod is not null)
        {
            conversion = Expression.Call(null, implicitMethod, sourceExpression);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to create an expression for conversion using TypeConverter.
    /// </summary>
    internal static bool TryCreateTypeConverterConversion(
        Expression sourceExpression,
        Type sourceType,
        Type targetType,
        out Expression conversion)
    {
        conversion = Expression.Empty();

        // Simplified use of TypeConverter - in a real implementation it would be more complex
        var sourceConverter = TypeDescriptor.GetConverter(sourceType);

        if (sourceConverter.CanConvertTo(targetType))
        {
            // Create a dynamic expression for calling TypeConverter.ConvertTo
            var converterVar = Expression.Variable(sourceConverter.GetType(), "converter");
            var expressions = new List<Expression>();

            // Create an instance of the converter
            expressions.Add(Expression.Assign(converterVar,
                Expression.Constant(sourceConverter)));

            // Call ConvertTo
            var convertToMethod = sourceConverter.GetType().GetMethod("ConvertTo", new[] { typeof(object), typeof(Type) })!;

            var result = Expression.Call(
                converterVar,
                convertToMethod,
                Expression.Convert(sourceExpression, typeof(object)),
                Expression.Constant(targetType)
            );

            // Convert the result to the target type
            expressions.Add(Expression.Convert(result, targetType));

            conversion = Expression.Block(new[] { converterVar }, expressions);
            return true;
        }

        var targetConverter = TypeDescriptor.GetConverter(targetType);

        if (targetConverter.CanConvertFrom(sourceType))
        {
            // Create a dynamic expression for calling TypeConverter.ConvertFrom
            var converterVar = Expression.Variable(targetConverter.GetType(), "converter");
            var expressions = new List<Expression>();

            // Create an instance of the converter
            expressions.Add(Expression.Assign(converterVar,
                Expression.Constant(targetConverter)));

            // Call ConvertFrom
            var convertFromMethod = targetConverter.GetType().GetMethod("ConvertFrom", new[] { typeof(object) })!;

            var result = Expression.Call(
                converterVar,
                convertFromMethod,
                Expression.Convert(sourceExpression, typeof(object))
            );

            // Convert the result to the target type
            expressions.Add(Expression.Convert(result, targetType));

            conversion = Expression.Block(new[] { converterVar }, expressions);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds a conversion operator between two types.
    /// </summary>
    internal static MethodInfo? FindConversionOperator(Type sourceType, Type targetType, string operatorName)
    {
        // Check source type
        var sourceMethods = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == operatorName)
            .Where(m => m.ReturnType == targetType)
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
            .FirstOrDefault();

        if (sourceMethods != null)
        {
            return sourceMethods;
        }

        // Check target type
        var targetMethod = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == operatorName)
            .Where(m => m.ReturnType == targetType)
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
            .FirstOrDefault();

        return targetMethod;
    }

    /// <summary>
    /// Determines whether the given type is primitive or a system type.
    /// </summary>
    internal static bool IsPrimitiveOrSystemType(Type type)
    {
        // Check nullable types
        if (TypeHelper.IsNullable(type, out var underlyingType) && underlyingType is Type)
        {
            type = underlyingType;
        }

        // Check if the type is in the list of primitive types
        if (TypesInfo.PrimitiveTypes.Contains(type))
        {
            return true;
        }

        // Check if the type is primitive
        return TypeHelper.IsPrimitiveType(type);
    }

    /// <summary>
    /// Determines whether it is an implicit conversion between two types.
    /// </summary>
    internal static bool IsImplicitConversion(Type sourceType, Type targetType)
    {
        if (ConversionsInfo.ImplicitConversions.TryGetValue(sourceType, out var targets))
            return targets.Contains(targetType);

        return false;
    }

    /// <summary>
    /// Determines whether it is an explicit conversion between two types.
    /// </summary>
    internal static bool IsExplicitConversion(Type sourceType, Type targetType)
    {
        if (ConversionsInfo.ExplicitConversions.TryGetValue(sourceType, out var targets))
            return targets.Contains(targetType);

        return false;
    }

    /// <summary>
    /// Gets the type name for the purpose of creating conversion method names.
    /// </summary>
    internal static string GetTypeName(Type type)
    {
        // If the type is nullable, get the name of its underlying type
        if (TypeHelper.IsNullable(type, out var underlyingType) && underlyingType is Type)
        {
            return GetTypeName(underlyingType);
        }

        if (type == typeof(int)) return "Int32";
        if (type == typeof(uint)) return "UInt32";
        if (type == typeof(long)) return "Int64";
        if (type == typeof(ulong)) return "UInt64";
        if (type == typeof(short)) return "Int16";
        if (type == typeof(ushort)) return "UInt16";
        if (type == typeof(byte)) return "Byte";
        if (type == typeof(sbyte)) return "SByte";
        if (type == typeof(float)) return "Single";
        if (type == typeof(double)) return "Double";
        if (type == typeof(decimal)) return "Decimal";
        if (type == typeof(bool)) return "Boolean";
        if (type == typeof(char)) return "Char";
        if (type == typeof(string)) return "String";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(DateTimeOffset)) return "DateTimeOffset";
        if (type == typeof(TimeSpan)) return "TimeSpan";
        if (type == typeof(DateOnly)) return "DateOnly";
        if (type == typeof(TimeOnly)) return "TimeOnly";
        if (type == typeof(Guid)) return "Guid";
        if (type == typeof(Type)) return "Type";
        if (type == typeof(Uri)) return "Uri";
        if (type == typeof(Version)) return "Version";
        if (type == typeof(nint)) return "IntPtr";
        if (type == typeof(nuint)) return "UIntPtr";

        // For other types, return their name
        return type.Name;
    }

    #endregion
}