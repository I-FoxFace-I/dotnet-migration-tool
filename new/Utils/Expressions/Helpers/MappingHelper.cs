using System.Linq.Expressions;
using System.Reflection;

namespace IvoEngine.Expressions.Helpers;

public enum MappingType
{
    Auto,
    Explicit,

}

//public class PropertyMapping
//{
//    public static string SourcePropertyName { get; set; }
//    public static string TargetPropertyName { get; set; }
//}

public static class MappingHelper
{
    /// <summary>
    /// Checks whether property data types are compatible for mapping.
    /// </summary>
    /// <param name="targetType">Data type of the target property</param>
    /// <param name="sourceType">Data type of the source property</param>
    /// <returns>True if types are compatible, otherwise False</returns>
    public static bool IsPropertyTypeCompatible(Type targetType, Type sourceType)
    {
        return IsPropertyTypeCompatibleInternal(targetType, sourceType);
    }

    /// <summary>
    /// Checks whether property data types are compatible for mapping.
    /// </summary>
    /// <param name="targetType">Data type of the target property</param>
    /// <param name="sourceType">Data type of the source property</param>
    /// <returns>True if types are compatible, otherwise False</returns>
    internal static bool IsPropertyTypeCompatibleInternal(Type targetType, Type sourceType)
    {
        return ConversionHelper.IsTypeConvertible(sourceType, targetType);

        // Basic check - types are the same
        if (targetType == sourceType)
        {
            return true;
        }

        // Check nullable types
        bool isTargetNullable = TypeHelper.IsNullable(targetType, out var targetUnderlyingType);
        bool isSourceNullable = TypeHelper.IsNullable(sourceType, out var sourceUnderlyingType);

        if (isSourceNullable && isTargetNullable)
        {
            return IsPropertyTypeCompatibleInternal(sourceUnderlyingType!, targetUnderlyingType!);
        }
        else if (isSourceNullable && !isTargetNullable)
        {
            return IsPropertyTypeCompatibleInternal(sourceUnderlyingType!, targetType);
        }
        else if (!isSourceNullable && isTargetNullable)
        {
            return IsPropertyTypeCompatible(sourceType, targetUnderlyingType!);
        }

        // Check collection types
        bool isTargetCollection = TypeHelper.IsCollectionType(targetType);
        bool isSourceCollection = TypeHelper.IsCollectionType(sourceType);

        if (isTargetCollection && isTargetCollection)
        {
            var sourceElementType = TypeHelper.GetElementType(targetType);
            var targetElementType = TypeHelper.GetElementType(sourceType);

            if (sourceElementType is not null && targetElementType is not null)
            {
                if (TypeHelper.IsKeyValuePair(sourceElementType) && TypeHelper.IsKeyValuePair(targetElementType))
                {
                    var sourceKeyType = sourceElementType.GetGenericArguments().ElementAt(0);
                    var targetKeyType = targetElementType.GetGenericArguments().ElementAt(0);
                    if (IsPropertyTypeCompatible(sourceKeyType, targetKeyType))
                    {
                        var sourceValueType = sourceElementType.GetGenericArguments().ElementAt(1);
                        var targetValueType = targetElementType.GetGenericArguments().ElementAt(1);

                        return IsPropertyTypeCompatible(sourceValueType, targetValueType);
                    }
                }
                else
                {
                    return IsPropertyTypeCompatibleInternal(sourceElementType, targetElementType);
                }
            }

            return false;
        }

        // Check if primitive types can be converted
        if (targetType.IsPrimitive && sourceType.IsPrimitive)
        {
            try
            {
                var parameter = Expression.Parameter(targetType, "p");
                var convert = Expression.Convert(parameter, sourceType);
                var lambda = Expression.Lambda(convert, parameter);
                lambda.Compile();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Check if implicit or explicit conversion exists
        var methods = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                   m.ReturnType == targetType &&
                   m.GetParameters().Length == 1 &&
                   m.GetParameters().First().ParameterType == sourceType)
            .ToList();

        if (methods.Any())
        {
            return true;
        }

        methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                   m.ReturnType == sourceType &&
                   m.GetParameters().Length == 1 &&
                   m.GetParameters().First().ParameterType == targetType)
            .ToList();

        if (methods.Any())
        {
            return true;
        }

        // Check if conversion is possible using TypeConverter
        var sourceConverter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
        if (sourceConverter.CanConvertTo(sourceType))
        {
            return true;
        }

        //var targetConverter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
        //if (targetConverter.CanConvertFrom(sourceType))
        //{
        //    return true;
        //}

        return false;
    }


}