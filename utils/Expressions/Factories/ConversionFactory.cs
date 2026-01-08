using Utils.Expressions.Helpers;
using Utils.Expressions.Conversion;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Utils.Expressions.Factories;

/// <summary>
/// Factory class for creating functions and delegates for conversions between data types.
/// </summary>
public static class ConversionFactory
{
    #region Conversion Functions

    /// <summary>
    /// Creates a function for converting a value from the source type to the target type.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <returns>Function for value conversion</returns>
    public static Func<TSource, TTarget> CreateConversionFunction<TSource, TTarget>()
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        // Get information about possible conversion
        var conversionInfo = ConversionHelper.GetConversionInfo(sourceType, targetType);

        if (!conversionInfo.CanConvert)
        {
            throw new InvalidOperationException($"Cannot create conversion function from type {sourceType.Name} to type {targetType.Name}. Conversion is not possible.");
        }

        // Input parameter for the conversion function
        var sourceParam = Expression.Parameter(sourceType, "source");

        // Create an expression for conversion based on available methods
        var conversionExpr = CreateConversionExpression(sourceParam, sourceType, targetType, conversionInfo);

        // Create and compile the lambda expression
        var lambda = Expression.Lambda<Func<TSource, TTarget>>(conversionExpr, sourceParam);
        return lambda.Compile();
    }

    /// <summary>
    /// Creates a function for converting a value between specified types.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Delegate for value conversion</returns>
    public static Delegate CreateConversionFunction(Type sourceType, Type targetType)
    {
        // Get information about possible conversion
        var conversionInfo = ConversionHelper.GetConversionInfo(sourceType, targetType);

        if (!conversionInfo.CanConvert)
        {
            throw new InvalidOperationException($"Cannot create conversion function from type {sourceType.Name} to type {targetType.Name}. Conversion is not possible.");
        }

        // Create a generic method to create a type-specific function
        var method = typeof(ConversionFactory).GetMethod(nameof(CreateConversionFunction),
            BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

        var genericMethod = method!.MakeGenericMethod(sourceType, targetType);
        return (Delegate)genericMethod.Invoke(null, null)!;
    }

    /// <summary>
    /// Creates a function for mapping complex objects from one type to another.
    /// </summary>
    /// <typeparam name="TSource">Source data type</typeparam>
    /// <typeparam name="TTarget">Target data type</typeparam>
    /// <returns>Function for object conversion</returns>
    public static Func<TSource, TTarget> CreateObjectMappingFunction<TSource, TTarget>()
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        // Check if types support property mapping
        if (!ConversionHelper.CanMapProperties(sourceType, targetType))
        {
            throw new InvalidOperationException($"Cannot create mapping function from type {sourceType.Name} to type {targetType.Name}. Types are not compatible for property mapping.");
        }

        // Input parameter for the mapping function
        var sourceParam = Expression.Parameter(sourceType, "source");

        // Output variable for the new instance of the target type
        var targetVar = Expression.Variable(targetType, "target");

        var expressions = new List<Expression>();

        // Null value check
        var nullCheck = Expression.Equal(sourceParam, Expression.Constant(null, sourceType));
        var defaultTarget = Expression.Default(targetType);

        // Create a new instance of the target type
        var newTarget = Expression.New(targetType);

        // Assign the new instance to the output variable
        expressions.Add(
            Expression.IfThenElse(
                nullCheck,
                Expression.Assign(targetVar, defaultTarget),
                Expression.Assign(targetVar, newTarget)
            )
        );

        // Get properties of the source and target types
        var sourceProperties = sourceType.GetProperties()
            .Where(p => p.CanRead && p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic)
            .ToList();

        var targetProperties = targetType.GetProperties()
            .Where(p => p.CanWrite && p.SetMethod != null && p.SetMethod.IsPublic && !p.SetMethod.IsStatic)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        // For each source property, find the corresponding property in the target type
        foreach (var sourceProp in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProp.Name, out var targetProp))
            {
                // Check if property can be converted
                if (ConversionHelper.IsTypeConvertible(sourceProp.PropertyType, targetProp.PropertyType))
                {
                    // Create an expression to get the value of the source property
                    var propValue = Expression.Property(sourceParam, sourceProp);

                    // Create an expression to check for null value of the source property
                    var propNullCheck = Expression.Equal(propValue, Expression.Constant(null, sourceProp.PropertyType));

                    // Create an expression for converting the value to the target property type
                    var convertedValue = CreatePropertyConversionExpression(propValue, sourceProp.PropertyType, targetProp.PropertyType);

                    // Create an expression for assigning the converted value to the target property
                    expressions.Add(
                        Expression.IfThen(
                            Expression.Not(nullCheck),
                            Expression.Assign(Expression.Property(targetVar, targetProp), convertedValue)
                        )
                    );
                }
            }
        }

        // Add an expression to return the output variable
        expressions.Add(targetVar);

        // Create and compile the lambda expression
        var lambda = Expression.Lambda<Func<TSource, TTarget>>(
            Expression.Block(new[] { targetVar }, expressions),
            sourceParam
        );

        return lambda.Compile();
    }

    /// <summary>
    /// Creates a function for mapping complex objects between specified types.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Delegate for object mapping</returns>
    public static Delegate CreateObjectMappingFunction(Type sourceType, Type targetType)
    {
        // Check if types support property mapping
        if (!ConversionHelper.CanMapProperties(sourceType, targetType))
        {
            throw new InvalidOperationException($"Cannot create mapping function from type {sourceType.Name} to type {targetType.Name}. Types are not compatible for property mapping.");
        }

        // Create a generic method to create a type-specific function
        var method = typeof(ConversionFactory).GetMethod(nameof(CreateObjectMappingFunction),
            BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

        var genericMethod = method!.MakeGenericMethod(sourceType, targetType);
        return (Delegate)genericMethod.Invoke(null, null)!;
    }

    /// <summary>
    /// Creates a function for collection conversion.
    /// </summary>
    /// <typeparam name="TSourceCollection">Source collection type</typeparam>
    /// <typeparam name="TTargetCollection">Target collection type</typeparam>
    /// <returns>Function for collection conversion</returns>
    public static Func<TSourceCollection, TTargetCollection> CreateCollectionConversionFunction<TSourceCollection, TTargetCollection>()
    {
        var sourceType = typeof(TSourceCollection);
        var targetType = typeof(TTargetCollection);

        // Check if they are collections
        if (!TypeHelper.IsCollectionType(sourceType) || !TypeHelper.IsCollectionType(targetType))
        {
            throw new InvalidOperationException($"Cannot create collection conversion function. Types {sourceType.Name} and {targetType.Name} are not collections.");
        }

        // Check if collections can be converted
        if (!ConversionHelper.CanConvertCollections(sourceType, targetType, false))
        {
            throw new InvalidOperationException($"Cannot create conversion function from collection {sourceType.Name} to collection {targetType.Name}. Conversion is not possible.");
        }

        // Get element types of collections
        var sourceElementType = TypeHelper.GetElementType(sourceType) ?? typeof(object);
        var targetElementType = TypeHelper.GetElementType(targetType) ?? typeof(object);

        // Create a function for converting individual elements
        var elementConverter = CreateElementConverterFunction(sourceElementType, targetElementType);

        // Input parameter for the conversion function
        var sourceParam = Expression.Parameter(sourceType, "source");

        // Create an expression for collection conversion
        var conversionExpr = CreateCollectionConversionExpression(sourceParam, sourceType, targetType, sourceElementType, targetElementType, elementConverter);

        // Create and compile the lambda expression
        var lambda = Expression.Lambda<Func<TSourceCollection, TTargetCollection>>(conversionExpr, sourceParam);
        return lambda.Compile();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates an expression for conversion based on available conversion methods.
    /// </summary>
    private static Expression CreateConversionExpression(Expression sourceExpr, Type sourceType, Type targetType, ConversionInfo conversionInfo)
    {
        // Check if types are the same (identity)
        if (conversionInfo.IsIdentity)
        {
            return sourceExpr;
        }

        // Check for null value conversion
        if (!sourceType.IsValueType)
        {
            var nullCheck = Expression.Equal(sourceExpr, Expression.Constant(null, sourceType));
            var defaultValue = Expression.Default(targetType);

            // For reference types, we must handle null value
            return Expression.Condition(
                nullCheck,
                defaultValue,
                CreateNonNullConversionExpression(sourceExpr, sourceType, targetType, conversionInfo)
            );
        }

        return CreateNonNullConversionExpression(sourceExpr, sourceType, targetType, conversionInfo);
    }

    /// <summary>
    /// Creates an expression for converting non-null values based on available conversion methods.
    /// </summary>
    private static Expression CreateNonNullConversionExpression(Expression sourceExpr, Type sourceType, Type targetType, ConversionInfo conversionInfo)
    {
        // Prioritize conversion methods based on reliability and performance

        // 1. Primitive type conversions
        if (conversionInfo.HasMethod(ConversionMethod.Primitive))
        {
            // For primitive types, we use direct conversion or specialized converters
            if (conversionInfo.IsImplicit)
            {
                return Expression.Convert(sourceExpr, targetType);
            }
            else
            {
                // We use ExplicitConverter for safer conversions
                var methodName = $"To{GetTypeName(targetType)}";
                var method = typeof(ExplicitConverter).GetMethod(methodName, new[] { sourceType });

                if (method != null)
                {
                    return Expression.Call(null, method, sourceExpr);
                }

                return Expression.Convert(sourceExpr, targetType);
            }
        }

        // 2. Enum type conversions
        if (conversionInfo.HasMethod(ConversionMethod.Enum))
        {
            if (sourceType.IsEnum && targetType.IsEnum)
            {
                // Same enum type
                return sourceExpr;
            }
            else if (sourceType.IsEnum)
            {
                // Enum to its underlying type or another type
                var enumUnderlyingType = Enum.GetUnderlyingType(sourceType);
                var enumValue = Expression.Convert(sourceExpr, enumUnderlyingType);

                if (enumUnderlyingType == targetType)
                {
                    return enumValue;
                }
                else if (targetType == typeof(string))
                {
                    // Enum to string
                    var toStringMethod = sourceType.GetMethod("ToString", Type.EmptyTypes)!;
                    return Expression.Call(sourceExpr, toStringMethod);
                }
                else
                {
                    // Enum to another type via underlying type
                    return Expression.Convert(enumValue, targetType);
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
                        sourceExpr,
                        Expression.Constant(true)
                    );

                    return Expression.Convert(parseCall, targetType);
                }
                else
                {
                    // Convert to underlying type and then to enum
                    var underlyingValue = Expression.Convert(sourceExpr, enumUnderlyingType);
                    return Expression.Convert(underlyingValue, targetType);
                }
            }
        }

        // 3. Conversions using operators
        if (conversionInfo.HasMethod(ConversionMethod.Operator))
        {
            // First, try to find an implicit operator
            var implicitOperator = FindConversionOperator(sourceType, targetType, "op_Implicit");
            if (implicitOperator != null)
            {
                return Expression.Call(null, implicitOperator, sourceExpr);
            }

            // If implicit operator not found, use explicit
            var explicitOperator = FindConversionOperator(sourceType, targetType, "op_Explicit");
            if (explicitOperator != null)
            {
                return Expression.Call(null, explicitOperator, sourceExpr);
            }
        }

        // 4. Conversions based on inheritance or interface implementation
        if (conversionInfo.HasMethod(ConversionMethod.Inheritance) ||
            conversionInfo.HasMethod(ConversionMethod.Interface))
        {
            if (targetType.IsAssignableFrom(sourceType))
            {
                // Upcasting - safe
                return Expression.Convert(sourceExpr, targetType);
            }
            else if (sourceType.IsAssignableFrom(targetType))
            {
                // Downcasting - potentially unsafe
                return Expression.Convert(sourceExpr, targetType);
            }
        }

        // 5. Conversions between generic types
        if (conversionInfo.HasMethod(ConversionMethod.Generic))
        {
            // For generic types, we usually need to create a new instance
            if (sourceType.IsGenericType && targetType.IsGenericType &&
                sourceType.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition())
            {
                var sourceGenericArgs = sourceType.GetGenericArguments();
                var targetGenericArgs = targetType.GetGenericArguments();

                // For simple cases, direct conversion is sufficient
                if (targetType.IsInterface && targetType.IsAssignableFrom(sourceType))
                {
                    return Expression.Convert(sourceExpr, targetType);
                }

                // For more complex cases, we would need to create a method for converting each generic argument
                // and then a new instance of the target type... this is a more complex case not implemented here
            }
        }

        // 6. Conversions using TypeConverter
        if (conversionInfo.HasMethod(ConversionMethod.TypeConverter))
        {
            var sourceConverter = TypeDescriptor.GetConverter(sourceType);
            if (sourceConverter.CanConvertTo(targetType))
            {
                var convertToMethod = sourceConverter.GetType().GetMethod("ConvertTo",
                    new[] { typeof(object), typeof(Type) })!;

                return Expression.Convert(
                    Expression.Call(
                        Expression.Constant(sourceConverter),
                        convertToMethod,
                        Expression.Convert(sourceExpr, typeof(object)),
                        Expression.Constant(targetType)
                    ),
                    targetType
                );
            }

            var targetConverter = TypeDescriptor.GetConverter(targetType);
            if (targetConverter.CanConvertFrom(sourceType))
            {
                var convertFromMethod = targetConverter.GetType().GetMethod("ConvertFrom",
                    new[] { typeof(object) })!;

                return Expression.Convert(
                    Expression.Call(
                        Expression.Constant(targetConverter),
                        convertFromMethod,
                        Expression.Convert(sourceExpr, typeof(object))
                    ),
                    targetType
                );
            }
        }

        // 7. Conversions using constructor
        if (conversionInfo.HasMethod(ConversionMethod.Constructor))
        {
            var constructor = targetType.GetConstructor(new[] { sourceType });
            if (constructor != null)
            {
                return Expression.New(constructor, sourceExpr);
            }
        }

        // 8. Property mapping for complex objects
        if (conversionInfo.HasMethod(ConversionMethod.PropertyMapping))
        {
            return CreatePropertyMappingExpression(sourceExpr, sourceType, targetType);
        }

        // If we reached here, use direct conversion (may fail at runtime)
        return Expression.Convert(sourceExpr, targetType);
    }

    /// <summary>
    /// Creates an expression for converting a property.
    /// </summary>
    private static Expression CreatePropertyConversionExpression(Expression propValue, Type sourceType, Type targetType)
    {
        // Get conversion information for the given property
        var conversionInfo = ConversionHelper.GetConversionInfo(sourceType, targetType);

        // Create a conversion expression
        return CreateConversionExpression(propValue, sourceType, targetType, conversionInfo);
    }

    /// <summary>
    /// Creates an expression for mapping properties between complex objects.
    /// </summary>
    private static Expression CreatePropertyMappingExpression(Expression sourceExpr, Type sourceType, Type targetType)
    {
        // Output variable for the new instance of the target type
        var targetVar = Expression.Variable(targetType, "mappedObj");

        var expressions = new List<Expression>();

        // Create a new instance of the target type
        var newTarget = Expression.New(targetType);

        // Assign the new instance to the output variable
        expressions.Add(Expression.Assign(targetVar, newTarget));

        // Get properties of the source and target types
        var sourceProperties = sourceType.GetProperties()
            .Where(p => p.CanRead && p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic)
            .ToList();

        var targetProperties = targetType.GetProperties()
            .Where(p => p.CanWrite && p.SetMethod != null && p.SetMethod.IsPublic && !p.SetMethod.IsStatic)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        // For each source property, find the corresponding property in the target type
        foreach (var sourceProp in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProp.Name, out var targetProp))
            {
                // Check if property can be converted
                if (ConversionHelper.IsTypeConvertible(sourceProp.PropertyType, targetProp.PropertyType))
                {
                    // Create an expression to get the value of the source property
                    var propValue = Expression.Property(sourceExpr, sourceProp);

                    // Create an expression for converting the value to the target property type
                    var convertedValue = CreatePropertyConversionExpression(propValue, sourceProp.PropertyType, targetProp.PropertyType);

                    // Create an expression for assigning the converted value to the target property
                    expressions.Add(Expression.Assign(Expression.Property(targetVar, targetProp), convertedValue));
                }
            }
        }

        // Add an expression to return the output variable
        expressions.Add(targetVar);

        return Expression.Block(new[] { targetVar }, expressions);
    }

    /// <summary>
    /// Creates an expression for collection conversion.
    /// </summary>
    private static Expression CreateCollectionConversionExpression(
        Expression sourceExpr,
        Type sourceType,
        Type targetType,
        Type sourceElementType,
        Type targetElementType,
        Delegate elementConverter)
    {
        // First, check if the source collection is null
        var nullCheck = Expression.Equal(sourceExpr, Expression.Constant(null, sourceType));
        var defaultTarget = Expression.Default(targetType);

        // If it's an array to array conversion, we can use Array.ConvertAll
        if (sourceType.IsArray && targetType.IsArray)
        {
            var convertAllMethod = typeof(Array).GetMethod("ConvertAll")!.MakeGenericMethod(sourceElementType, targetElementType);
            var converterType = typeof(Converter<,>).MakeGenericType(sourceElementType, targetElementType);
            var converterParam = Expression.Constant(elementConverter);

            var convertCall = Expression.Call(null, convertAllMethod, sourceExpr, converterParam);

            return Expression.Condition(nullCheck, defaultTarget, convertCall);
        }

        // If the target type has a constructor that accepts IEnumerable<TElement>
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(targetElementType);
        var constructor = targetType.GetConstructor(new[] { enumerableType });

        if (constructor != null)
        {
            // We need to convert elements from the source collection to elements of the target collection
            var selectMethod = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .First()
                .MakeGenericMethod(sourceElementType, targetElementType);

            var converterParam = Expression.Constant(elementConverter);

            var selectCall = Expression.Call(null, selectMethod, sourceExpr, converterParam);
            var newCollection = Expression.New(constructor, selectCall);

            return Expression.Condition(nullCheck, defaultTarget, newCollection);
        }

        // If the target type has a static method for creating from IEnumerable<TElement>
        var fromMethod = targetType.GetMethod("From", BindingFlags.Public | BindingFlags.Static, null,
            new[] { enumerableType }, null);

        if (fromMethod != null)
        {
            var selectMethod = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .First()
                .MakeGenericMethod(sourceElementType, targetElementType);

            var converterParam = Expression.Constant(elementConverter);

            var selectCall = Expression.Call(null, selectMethod, sourceExpr, converterParam);
            var fromCall = Expression.Call(null, fromMethod, selectCall);

            return Expression.Condition(nullCheck, defaultTarget, fromCall);
        }

        // If the target type is a list, we can use ToList
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var selectMethod = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .First()
                .MakeGenericMethod(sourceElementType, targetElementType);

            var toListMethod = typeof(Enumerable).GetMethod("ToList")!.MakeGenericMethod(targetElementType);

            var converterParam = Expression.Constant(elementConverter);

            var selectCall = Expression.Call(null, selectMethod, sourceExpr, converterParam);
            var toListCall = Expression.Call(null, toListMethod, selectCall);

            return Expression.Condition(nullCheck, defaultTarget, toListCall);
        }

        // If the target type is an array, we can use ToArray
        if (targetType.IsArray)
        {
            var selectMethod = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .First()
                .MakeGenericMethod(sourceElementType, targetElementType);

            var toArrayMethod = typeof(Enumerable).GetMethod("ToArray")!.MakeGenericMethod(targetElementType);

            var converterParam = Expression.Constant(elementConverter);

            var selectCall = Expression.Call(null, selectMethod, sourceExpr, converterParam);
            var toArrayCall = Expression.Call(null, toArrayMethod, selectCall);

            return Expression.Condition(nullCheck, defaultTarget, toArrayCall);
        }

        // If we couldn't find a better way, use direct conversion
        return Expression.Convert(sourceExpr, targetType);
    }

    /// <summary>
    /// Creates a function for converting individual elements of collections.
    /// </summary>
    private static Delegate CreateElementConverterFunction(Type sourceElementType, Type targetElementType)
    {
        // Create a delegate for converting individual elements
        var sourceParam = Expression.Parameter(sourceElementType, "element");

        var conversionInfo = ConversionHelper.GetConversionInfo(sourceElementType, targetElementType);
        var conversionExpr = CreateConversionExpression(sourceParam, sourceElementType, targetElementType, conversionInfo);

        var delegateType = typeof(Converter<,>).MakeGenericType(sourceElementType, targetElementType);
        var lambda = Expression.Lambda(delegateType, conversionExpr, sourceParam);

        return lambda.Compile();
    }

    /// <summary>
    /// Finds a conversion operator in the specified types.
    /// </summary>
    private static MethodInfo? FindConversionOperator(Type sourceType, Type targetType, string operatorName)
    {
        // Check operators in the source type
        var sourceMethods = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == operatorName)
            .Where(m => m.ReturnType == targetType)
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
            .FirstOrDefault();

        if (sourceMethods != null)
        {
            return sourceMethods;
        }

        // Check operators in the target type
        var targetMethods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == operatorName)
            .Where(m => m.ReturnType == targetType)
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
            .FirstOrDefault();

        return targetMethods;
    }

    /// <summary>
    /// Gets the type name for creating conversion method names.
    /// </summary>
    private static string GetTypeName(Type type)
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
