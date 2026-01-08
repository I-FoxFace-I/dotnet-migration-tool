using Utils.Expressions.Conversion;
using System.Reflection;
using System.ComponentModel;
using Utils.Expressions.StaticData;

namespace Utils.Expressions.Helpers;

/// <summary>
/// Provides methods for examining conversion possibilities between data types.
/// </summary>
public static class ConversionHelper
{
    private const string ImplicitOperatorName = "op_Implicit";
    private const string ExplicitOperatorName = "op_Explicit";

    /// <summary>
    /// Determines whether it is possible to convert a value from one type to another.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if conversion is possible, otherwise False</returns>
    public static bool IsTypeConvertible(Type sourceType, Type targetType, bool implicitOnly = false)
    {
        // Check null parameters
        if (sourceType == null || targetType == null)
        {
            return false;
        }

        if (targetType == typeof(object))
        {
            return true;
        }

        if (implicitOnly)
        {
            if(sourceType == typeof(object))
            {
                return false;
            }
        }

        // If types are the same, conversion is not needed (or is trivial)
        if (sourceType == targetType)
        {
            return true;
        }

        // Check nullable types
        bool isSourceNullable = TypeHelper.IsNullable(sourceType, out var sourceUnderlyingType);
        bool isTargetNullable = TypeHelper.IsNullable(targetType, out var targetUnderlyingType);

        // Conversion between nullable types
        if (isSourceNullable && isTargetNullable)
        {
            return IsTypeConvertible(sourceUnderlyingType!, targetUnderlyingType!);
        }
        else if (isSourceNullable && !isTargetNullable)
        {
            return IsTypeConvertible(sourceUnderlyingType!, targetType);
        }
        else if (!isSourceNullable && isTargetNullable)
        {
            return IsTypeConvertible(sourceType, targetUnderlyingType!);
        }

        // Check basic conversions between types
        if (CanConvertPrimitiveTypes(sourceType, targetType, implicitOnly))
        {
            return true;
        }

        // Check enum types
        if (CanConvertEnumTypes(sourceType, targetType))
        {
            return true;
        }

        // Check collection types
        if (TypeHelper.IsCollectionType(sourceType) && TypeHelper.IsCollectionType(targetType))
        {
            return CanConvertCollections(sourceType, targetType, implicitOnly);
        }

        // Check conversions using operators
        if (CanConvertUsingOperator(sourceType, targetType))
        {
            return true;
        }

        // Check conversions based on inheritance
        if (CanConvertByInheritance(sourceType, targetType, implicitOnly))
        {
            return true;
        }

        // Check conversions based on interface implementation
        if (CanConvertByInterfaceImplementation(sourceType, targetType))
        {
            return true;
        }

        // Check conversions using constructor
        if (CanConvertUsingConstructor(sourceType, targetType))
        {
            return true;
        }

        // Check generic types
        if (CanConvertGenericTypes(sourceType, targetType, implicitOnly))
        {
            return true;
        }

        // Check property mapping for complex objects
        if (CanMapProperties(sourceType, targetType))
        {
            return true;
        }

        if (!implicitOnly)
        {
            // Check conversions using TypeConverter
            if (CanConvertUsingTypeConverter(sourceType, targetType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether it is possible to convert between primitive types.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if conversion is possible, otherwise False</returns>
    public static bool CanConvertPrimitiveTypes(Type sourceType, Type targetType, bool implicitOnly = false)
    {
        // Check if both types are primitive or system types
        if (!TypeHelper.IsPrimitiveType(sourceType) || !TypeHelper.IsPrimitiveType(targetType))
        {
            return false;
        }

        // Check conversion matrix for primitive types
        if (ConversionsInfo.PrimitiveTypeIndexes.TryGetValue(sourceType, out int sourceIndex) && ConversionsInfo.PrimitiveTypeIndexes.TryGetValue(targetType, out int targetIndex))
        {
            if (implicitOnly)
            {
                return ConversionsInfo.ConversionMatrix[sourceIndex, targetIndex] == ConversionType.Implicit;
            }
            else
            {
                return ConversionsInfo.ConversionMatrix[sourceIndex, targetIndex] != ConversionType.NotPossible;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether it is possible to convert between collections.
    /// </summary>
    /// <param name="sourceType">Source collection data type</param>
    /// <param name="targetType">Target collection data type</param>
    /// <returns>True if conversion is possible, otherwise False</returns>
    public static bool CanConvertCollections(Type sourceType, Type targetType, bool implicitOnly = false)
    {
        var sourceElementType = TypeHelper.GetElementType(sourceType);
        var targetElementType = TypeHelper.GetElementType(targetType);

        // If element types of collections cannot be determined, conversion is not possible
        if (sourceElementType is null || targetElementType is null)
        {
            return false;
        }

        // For Dictionary (KeyValuePair collection)
        if (TypeHelper.IsKeyValuePair(sourceElementType) && TypeHelper.IsKeyValuePair(targetElementType))
        {
            return CanConvertGenericTypes(sourceElementType, targetElementType, implicitOnly);
        }

        // For regular collections, it's sufficient that element types can be converted
        return IsTypeConvertible(sourceElementType, targetElementType, implicitOnly);
    }

    /// <summary>
    /// Determines whether it is possible to convert using a conversion operator.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if an implicit or explicit conversion operator exists, otherwise False</returns>
    public static bool CanConvertUsingOperator(Type sourceType, Type targetType, bool implicitOnly = false)
    {
        // Check implicit and explicit conversion operators in both types

        // Check implicit operator in the source type
        var implicitOperatorInSource = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == ImplicitOperatorName)
            .Where(m => m.ReturnType == targetType)
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
            .Any();

        if (implicitOperatorInSource)
        {
            return true;
        }

        if (!implicitOnly)
        {
            // Check explicit operator in the source type
            var explicitOperatorInSource = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == ExplicitOperatorName)
                .Where(m => m.ReturnType == targetType)
                .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
                .Any();

            if (explicitOperatorInSource)
            {
                return true;
            }
        }


        // Check implicit operator in the target type
        var implicitOperatorInTarget = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == ImplicitOperatorName)
            .Where(m => m.ReturnType == targetType)
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
            .Any();

        if(implicitOperatorInSource)
        {
            return true;
        }

        if (!implicitOnly)
        {
            // Check explicit operator in the target type
            var explicitOperatorInTarget = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == ExplicitOperatorName)
                .Where(m => m.ReturnType == targetType)
                .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
                .Any();

            if (explicitOperatorInTarget)
            {
                return true;
            }
        }

        return false;

    }

    /// <summary>
    /// Determines whether it is possible to convert using TypeConverter.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if a TypeConverter exists that allows conversion, otherwise False</returns>
    public static bool CanConvertUsingTypeConverter(Type sourceType, Type targetType)
    {
        // Check if TypeConverter of the source type can convert to the target type
        var sourceConverter = TypeDescriptor.GetConverter(sourceType);
        if (sourceConverter != null && sourceConverter.CanConvertTo(targetType))
        {
            return true;
        }

        // Check if TypeConverter of the target type can convert from the source type
        var targetConverter = TypeDescriptor.GetConverter(targetType);
        if (targetConverter != null && targetConverter.CanConvertFrom(sourceType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether it is possible to convert on the basis of inheritance.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if conversion is possible on the basis of inheritance, otherwise False</returns>
    public static bool CanConvertByInheritance(Type sourceType, Type targetType, bool implicitOnly = false)
    {
        // Check if the target type is a parent of the source type (upcasting)
        if (targetType.IsAssignableFrom(sourceType))
        {
            return true;
        }

        if (!implicitOnly)
        {
            // Check if the source type is a parent of the target type (downcasting)
            // Downcasting is possible, but potentially dangerous and requires explicit conversion
            if (sourceType.IsAssignableFrom(targetType))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines whether it is possible to convert on the basis of the fact that the target type is an interface and the source type is its implementation.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if conversion is possible on the basis of interface implementation, otherwise False</returns>
    public static bool CanConvertByInterfaceImplementation(Type sourceType, Type targetType)
    {
        // Check if the target type is an interface implemented by the source type
        if (targetType.IsInterface && sourceType.GetInterfaces().Contains(targetType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether it is possible to convert using a constructor of the target type that accepts the source type.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if a suitable constructor exists, otherwise False</returns>
    public static bool CanConvertUsingConstructor(Type sourceType, Type targetType)
    {
        // Check if the target type has a constructor that accepts the source type
        if (targetType.GetConstructor(new[] { sourceType }) is not null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether it is possible to convert between enum types, or between an enum and a primitive type.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if conversion is possible, otherwise False</returns>
    public static bool CanConvertEnumTypes(Type sourceType, Type targetType)
    {
        // Conversion from enum to enum
        if (sourceType.IsEnum && targetType.IsEnum)
        {
            // Only if it's the same enum type
            return sourceType == targetType;
        }

        // Conversion from enum to primitive type
        if (sourceType.IsEnum && TypeHelper.IsPrimitiveType(targetType))
        {
            // Enum can be safely converted only to its underlying type
            var enumUnderlyingType = Enum.GetUnderlyingType(sourceType);

            if (enumUnderlyingType == targetType || targetType == TypesInfo.StringType)
                return true;

            // Also to string (ToString method)
            if (CanConvertImplicitly(enumUnderlyingType, targetType))
                return true;

            return false;
        }

        // Conversion from primitive type to enum
        if (TypeHelper.IsPrimitiveType(sourceType) && targetType.IsEnum)
        {
            // Primitive type can be safely converted to enum only if
            // it's the underlying type of the enum or string
            var enumUnderlyingType = Enum.GetUnderlyingType(targetType);

            if (sourceType == enumUnderlyingType || sourceType == TypesInfo.StringType)
                return true;

            // String to enum via Enum.Parse
            if (CanConvertImplicitly(sourceType, enumUnderlyingType))
                return true;

            return false;
        }

        return false;
    }

    /// <summary>
    /// Determines whether it is possible to convert between generic types.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if conversion is possible, otherwise False</returns>
    public static bool CanConvertGenericTypes(Type sourceType, Type targetType, bool implicitOnly = false)
    {
        // Check if both types are generic
        if (!sourceType.IsGenericType || !targetType.IsGenericType)
        {
            return false;
        }

        // Get generic definitions of types
        var sourceGenericTypeDefinition = sourceType.GetGenericTypeDefinition();
        var targetGenericTypeDefinition = targetType.GetGenericTypeDefinition();

        // If it's the same generic types, we check if we can convert their generic parameters
        if (sourceGenericTypeDefinition == targetGenericTypeDefinition)
        {
            var sourceGenericArgs = sourceType.GetGenericArguments();
            var targetGenericArgs = targetType.GetGenericArguments();

            // Must have the same number of generic arguments
            if (sourceGenericArgs.Length != targetGenericArgs.Length)
            {
                return false;
            }

            // Check conversion possibility for each pair of generic arguments
            for (int i = 0; i < sourceGenericArgs.Length; i++)
            {
                if (!IsTypeConvertible(sourceGenericArgs[i], targetGenericArgs[i], implicitOnly))
                {
                    return false;
                }
            }

            return true;
        }

        // This could be another check for specific pairs of generic types,
        // for example, conversion between different collections etc.

        return false;
    }

    /// <summary>
    /// Determines whether it is possible to map properties from the source type to the target type.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if mapping is possible, otherwise False</returns>
    public static bool CanMapProperties(Type sourceType, Type targetType)
    {
        // If it's primitive types or collections, it's not property mapping
        if (TypeHelper.IsPrimitiveType(sourceType) || TypeHelper.IsPrimitiveType(targetType) ||
            TypeHelper.IsCollectionType(sourceType) || TypeHelper.IsCollectionType(targetType))
        {
            return false;
        }

        // Check if the target type has a parameterless constructor
        bool hasDefaultConstructor = targetType.GetConstructor(Type.EmptyTypes) != null;

        if (!hasDefaultConstructor)
        {
            return false;
        }

        // Get properties of the source and target types
        var sourceProperties = sourceType.GetProperties()
            .Where(p => p.CanRead && p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic)
            .ToList();

        var targetProperties = targetType.GetProperties()
            .Where(p => p.CanWrite && p.SetMethod != null && p.SetMethod.IsPublic && !p.SetMethod.IsStatic)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        // Check if at least one property with compatible types exists
        bool foundMatchingProperty = true;



        // Check if all required properties of the target type have corresponding properties in the source type
        var requiredTargetProperties = targetProperties.Values
            .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(System.Runtime.CompilerServices.RequiredMemberAttribute)))
            .ToList();

        foreach (var requiredProp in requiredTargetProperties)
        {
            var matchingSourceProp = sourceProperties.FirstOrDefault(p =>
                string.Equals(p.Name, requiredProp.Name, StringComparison.OrdinalIgnoreCase));

            if (matchingSourceProp == null || !IsTypeConvertible(matchingSourceProp.PropertyType, requiredProp.PropertyType))
            {
                return false;
            }
        }

        foreach (var sourceProp in sourceProperties.Where(p => !targetProperties.ContainsKey(p.Name)))
        {
            if (targetProperties.TryGetValue(sourceProp.Name, out var targetProp))
            {
                if (!IsTypeConvertible(sourceProp.PropertyType, targetProp.PropertyType))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Determines the type of conversion between the source and target types.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Type of conversion (NotPossible, Implicit, Explicit)</returns>
    public static ConversionType GetConversionType(Type sourceType, Type targetType)
    {
        // If types are the same, no conversion is needed
        if (sourceType == targetType)
        {
            return ConversionType.Implicit;
        }

        // Check primitive types
        if (TypeHelper.IsPrimitiveType(sourceType) && TypeHelper.IsPrimitiveType(targetType))
        {
            if (ConversionsInfo.PrimitiveTypeIndexes.TryGetValue(sourceType, out int sourceIndex) &&
                ConversionsInfo.PrimitiveTypeIndexes.TryGetValue(targetType, out int targetIndex))
            {
                return ConversionsInfo.ConversionMatrix[sourceIndex, targetIndex];
            }
        }

        // Check implicit conversions
        if (CanConvertImplicitly(sourceType, targetType))
        {
            return ConversionType.Implicit;
        }

        // Check explicit conversions
        if (IsTypeConvertible(sourceType, targetType))
        {
            return ConversionType.Explicit;
        }

        return ConversionType.NotPossible;
    }

    /// <summary>
    /// Determines whether an implicit conversion between the source and target types is possible.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>True if an implicit conversion is possible, otherwise False</returns>
    public static bool CanConvertImplicitly(Type sourceType, Type targetType)
    {
        if (sourceType is null || targetType is null)
        {
            return false;
        }
        if(targetType == typeof(object))
        {
            return true;
        }
        if(sourceType == typeof(object))
        {
            return false;
        }
        // Same types
        if (sourceType == targetType)
        {
            return true;
        }

        // Check nullable types
        bool isSourceNullable = TypeHelper.IsNullable(sourceType, out var sourceUnderlyingType);
        bool isTargetNullable = TypeHelper.IsNullable(targetType, out var targetUnderlyingType);

        // Conversion between nullable types
        if (isSourceNullable && isTargetNullable)
        {
            return CanConvertImplicitly(sourceUnderlyingType!, targetUnderlyingType!);
        }
        else if (!isSourceNullable && isTargetNullable)
        {
            return CanConvertImplicitly(sourceType, targetUnderlyingType!);
        }
        else if (isSourceNullable && !isTargetNullable)
        {
            return false;
        }


        // Implicit conversion of primitive types
        if (TypeHelper.IsPrimitiveType(sourceType) && TypeHelper.IsPrimitiveType(targetType))
        {
            if (ConversionsInfo.ImplicitConversions.TryGetValue(sourceType, out var targets) && targets.Contains(targetType))
            {
                return true;
            }
        }

        if (TypeHelper.IsCollectionType(sourceType) && TypeHelper.IsCollectionType(targetType))
        {
            return CanConvertCollections(sourceType, targetType, true);
        }

        // Check upcasting (inheritance)
        if (targetType.IsAssignableFrom(sourceType))
        {
            return true;
        }

        if(CanConvertUsingOperator(sourceType, targetType, true))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets detailed information about conversion possibilities between two types.
    /// </summary>
    /// <param name="sourceType">Source data type</param>
    /// <param name="targetType">Target data type</param>
    /// <returns>Object containing conversion information</returns>
    public static ConversionInfo GetConversionInfo(Type sourceType, Type targetType)
    {
        // Check null parameters
        if (sourceType == null || targetType == null)
        {
            return new ConversionInfo(
                sourceType,
                targetType,
                false,
                false,
                Conversion.ConversionType.NotPossible,
                ConversionMethod.None
            );
        }

        // Initialize available conversion methods
        var conversionMethods = ConversionMethod.None;
        bool implicitlyConvertible = false;

        // Check identity (same types)
        if (sourceType == targetType)
        {
            conversionMethods |= ConversionMethod.Identity | ConversionMethod.ImplicitCast;
            implicitlyConvertible = true;
        }

        // Check conversion to Object
        if (targetType == typeof(object) && sourceType != typeof(object))
        {
            conversionMethods |= ConversionMethod.Inheritance | ConversionMethod.ImplicitCast;
            implicitlyConvertible = true;
        }

        // Check nullable types
        bool isSourceNullable = TypeHelper.IsNullable(sourceType, out var sourceUnderlyingType);
        bool isTargetNullable = TypeHelper.IsNullable(targetType, out var targetUnderlyingType);

        if (isSourceNullable || isTargetNullable)
        {
            conversionMethods |= ConversionMethod.Nullable;

            // Non-nullable to nullable of the same type is implicit
            if (!isSourceNullable && isTargetNullable && sourceType == targetUnderlyingType)
            {
                conversionMethods |= ConversionMethod.ImplicitCast;
                implicitlyConvertible = true;
            }
            // Nullable to non-nullable is explicit
            else if (isSourceNullable && !isTargetNullable && sourceUnderlyingType == targetType)
            {
                conversionMethods |= ConversionMethod.ExplicitCast;
            }
        }

        // Check primitive types
        if (CanConvertPrimitiveTypes(sourceType, targetType, false))
        {
            conversionMethods |= ConversionMethod.Primitive;

            if (CanConvertPrimitiveTypes(sourceType, targetType, true))
            {
                conversionMethods |= ConversionMethod.ImplicitCast;
                implicitlyConvertible = true;
            }
            else
            {
                conversionMethods |= ConversionMethod.ExplicitCast;
            }
        }

        // Check enum types
        if (CanConvertEnumTypes(sourceType, targetType))
        {
            conversionMethods |= ConversionMethod.Enum;

            if (CanConvertEnumTypes(sourceType, targetType))
            {
                conversionMethods |= ConversionMethod.ImplicitCast;
                implicitlyConvertible = true;
            }
            else
            {
                conversionMethods |= ConversionMethod.ExplicitCast;
            }
        }

        // Check collection types
        if (TypeHelper.IsCollectionType(sourceType) && TypeHelper.IsCollectionType(targetType) &&
            CanConvertCollections(sourceType, targetType, false))
        {
            conversionMethods |= ConversionMethod.Collection;

            if (CanConvertCollections(sourceType, targetType, true))
            {
                conversionMethods |= ConversionMethod.ImplicitCast;
                implicitlyConvertible = true;
            }
            else
            {
                conversionMethods |= ConversionMethod.ExplicitCast;
            }
        }

        // Check conversion operators
        if (CanConvertUsingOperator(sourceType, targetType, false))
        {
            conversionMethods |= ConversionMethod.Operator;

            if (CanConvertUsingOperator(sourceType, targetType, true))
            {
                conversionMethods |= ConversionMethod.ImplicitCast;
                implicitlyConvertible = true;
            }
            else
            {
                conversionMethods |= ConversionMethod.ExplicitCast;
            }
        }

        // Check inheritance
        if (CanConvertByInheritance(sourceType, targetType, false))
        {
            conversionMethods |= ConversionMethod.Inheritance;

            if (CanConvertByInheritance(sourceType, targetType, true))
            {
                conversionMethods |= ConversionMethod.ImplicitCast;
                implicitlyConvertible = true;
            }
            else
            {
                conversionMethods |= ConversionMethod.ExplicitCast;
            }
        }

        // Check interface implementation
        if (CanConvertByInterfaceImplementation(sourceType, targetType))
        {
            conversionMethods |= ConversionMethod.Interface | ConversionMethod.ImplicitCast;
            implicitlyConvertible = true;
        }

        // Check generic types
        if (CanConvertGenericTypes(sourceType, targetType, false))
        {
            conversionMethods |= ConversionMethod.Generic;

            if (CanConvertGenericTypes(sourceType, targetType, true))
            {
                conversionMethods |= ConversionMethod.ImplicitCast;
                implicitlyConvertible = true;
            }
            else
            {
                conversionMethods |= ConversionMethod.ExplicitCast;
            }
        }

        // Check property mapping
        if (CanMapProperties(sourceType, targetType))
        {
            conversionMethods |= ConversionMethod.PropertyMapping | ConversionMethod.ExplicitCast;
        }

        // Check TypeConverter
        if (CanConvertUsingTypeConverter(sourceType, targetType))
        {
            conversionMethods |= ConversionMethod.TypeConverter | ConversionMethod.ExplicitCast;
        }

        // Check constructors
        if (CanConvertUsingConstructor(sourceType, targetType))
        {
            conversionMethods |= ConversionMethod.Constructor | ConversionMethod.ExplicitCast;
        }

        // Determine conversion type
        var conversionType = Conversion.ConversionType.NotPossible;
        bool canConvert = conversionMethods != ConversionMethod.None;

        if (canConvert)
        {
            conversionType = implicitlyConvertible
                ? Conversion.ConversionType.Implicit
                : Conversion.ConversionType.Explicit;
        }

        return new ConversionInfo(
            sourceType,
            targetType,
            canConvert,
            implicitlyConvertible,
            conversionType,
            conversionMethods
        );
    }
}

/// <summary>
/// Defines possible ways to convert between types.
/// </summary>
[Flags]
public enum ConversionMethod
{
    /// <summary>
    /// Conversion is not possible.
    /// </summary>
    None = 0,

    /// <summary>
    /// Conversion between the same types (identity).
    /// </summary>
    Identity = 1,

    /// <summary>
    /// Conversion between primitive types.
    /// </summary>
    Primitive = 2,

    /// <summary>
    /// Conversion between enum types or between an enum and a primitive type.
    /// </summary>
    Enum = 4,

    /// <summary>
    /// Conversion between collections.
    /// </summary>
    Collection = 8,

    /// <summary>
    /// Conversion using an operator (implicit or explicit).
    /// </summary>
    Operator = 16,

    /// <summary>
    /// Conversion based on inheritance (upcasting or downcasting).
    /// </summary>
    Inheritance = 32,

    /// <summary>
    /// Conversion based on interface implementation.
    /// </summary>
    Interface = 64,

    /// <summary>
    /// Conversion between generic types.
    /// </summary>
    Generic = 128,

    /// <summary>
    /// Conversion using property mapping.
    /// </summary>
    PropertyMapping = 256,

    /// <summary>
    /// Conversion using TypeConverter.
    /// </summary>
    TypeConverter = 512,

    /// <summary>
    /// Conversion using a constructor of the target type.
    /// </summary>
    Constructor = 1024,

    /// <summary>
    /// Conversion is possible, but only explicitly (requires cast).
    /// </summary>
    ExplicitCast = 2048,

    /// <summary>
    /// Conversion is possible implicitly (no need for cast).
    /// </summary>
    ImplicitCast = 4096,

    /// <summary>
    /// Conversion between nullable types.
    /// </summary>
    Nullable = 8192
}

/// <summary>
/// Contains detailed information about conversion possibilities between types.
/// </summary>
public class ConversionInfo
{
    /// <summary>
    /// Source type for conversion.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// Target type of conversion.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Indicates whether conversion is possible.
    /// </summary>
    public bool CanConvert { get; }

    /// <summary>
    /// Indicates whether conversion is possible implicitly.
    /// </summary>
    public bool CanConvertImplicitly { get; }

    /// <summary>
    /// Type of conversion according to the ConversionType classification.
    /// </summary>
    public ConversionType ConversionType { get; }

    /// <summary>
    /// Methods by which conversion can be performed.
    /// </summary>
    public ConversionMethod ConversionMethods { get; }

    /// <summary>
    /// Creates a new instance with conversion information.
    /// </summary>
    public ConversionInfo(
        Type sourceType,
        Type targetType,
        bool canConvert,
        bool canConvertImplicitly,
        ConversionType conversionType,
        ConversionMethod conversionMethods)
    {
        SourceType = sourceType;
        TargetType = targetType;
        CanConvert = canConvert;
        CanConvertImplicitly = canConvertImplicitly;
        ConversionType = conversionType;
        ConversionMethods = conversionMethods;
    }

    /// <summary>
    /// Returns whether a specific conversion method is available.
    /// </summary>
    public bool HasMethod(ConversionMethod method)
    {
        return (ConversionMethods & method) == method;
    }

    /// <summary>
    /// Returns whether conversion is implicit.
    /// </summary>
    public bool IsImplicit => ConversionType == Conversion.ConversionType.Implicit;

    /// <summary>
    /// Returns whether conversion is explicit.
    /// </summary>
    public bool IsExplicit => ConversionType == Conversion.ConversionType.Explicit;

    /// <summary>
    /// Returns whether it's a conversion between the same types.
    /// </summary>
    public bool IsIdentity => HasMethod(ConversionMethod.Identity);

    /// <summary>
    /// Returns whether it's a conversion based on inheritance.
    /// </summary>
    public bool IsInheritanceBased => HasMethod(ConversionMethod.Inheritance);

    /// <summary>
    /// Returns whether it's a primitive conversion.
    /// </summary>
    public bool IsPrimitiveConversion => HasMethod(ConversionMethod.Primitive);

    /// <summary>
    /// Returns whether it's a collection conversion.
    /// </summary>
    public bool IsCollectionConversion => HasMethod(ConversionMethod.Collection);

    /// <summary>
    /// Returns whether it's a conversion using property mapping.
    /// </summary>
    public bool UsesPropertyMapping => HasMethod(ConversionMethod.PropertyMapping);
}