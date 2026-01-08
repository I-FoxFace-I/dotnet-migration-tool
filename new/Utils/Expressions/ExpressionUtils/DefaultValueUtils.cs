using System.Collections;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using IvoEngine.Expressions.Helpers;
using IvoEngine.Expressions.StaticData;

namespace IvoEngine.Expressions.ExpressionUtils;

public static class DefaultValueUtils
{
    /// <summary>
    /// Returns an Expression that creates a default value for the given data type.
    /// Used for variables that are required or are not nullable by default.
    /// Necessary for strings, classes, and collections.
    /// </summary>
    /// <param name="propertyType">Type of the property for which to create a default value</param>
    /// <returns>Expression representing the default value of the given type</returns>
    public static Expression DefaultValue(Type propertyType)
    {
        return CreateDefaultValue(propertyType);
    }

    /// <summary>
    /// Returns an Expression that creates a default value for the given data type.
    /// Used for variables that are required or are not nullable by default.
    /// Necessary for strings, classes, and collections.
    /// </summary>
    /// <param name="propertyType">Type of the property for which to create a default value</param>
    /// <returns>Expression representing the default value of the given type</returns>
    internal static Expression CreateDefaultValue(Type propertyType)
    {
                    // 1. Basic types
            if (propertyType == typeof(string))
            {
                return Expression.Constant(string.Empty);
            }
        else if (propertyType.IsPrimitive)
        {
            return Expression.Default(propertyType);
        }
        // 2. Collections and arrays
        else if (TypeHelper.IsCollectionType(propertyType))
        {
            return CreateCollectionDefaultValue(propertyType);
        }
        // 3. Value types (structures)
        else if (propertyType.IsValueType)
        {
            return Expression.Default(propertyType);
        }
        // 4. Reference types (classes)
        else if (propertyType.IsClass)
        {
            return CreateClassDefaultValue(propertyType);
        }
        // 5. Other types
        else
        {
            return CreateOtherDefaultValue(propertyType);
        }
    }

    /// <summary>
    /// Creates an expression for the default value of a collection.
    /// </summary>
    internal static Expression CreateCollectionDefaultValue(Type propertyType)
    {
        if (propertyType.IsInterface)
        {
            if (TypeHelper.IsSystemCollectionInterface(propertyType))
            {
                return TypeUtils.DefaultCollection(propertyType);
            }
            else
            {
                return CustomInterfaceDefaultCollection(propertyType);
            }

        }
        else
        {
            return CreateConcreteCollectionDefaultValue(propertyType);
        }
    }

    /// <summary>
    /// Creates an expression for the default value of a custom collection interface.
    /// </summary>
    internal static Expression CustomInterfaceDefaultCollection(Type interfaceType)
    {
        Expression defaultCollection = TypeUtils.DefaultInterfaceValue(interfaceType, out var isNull);

        if (isNull && interfaceType.IsGenericType)
        {
            var targetInterface = interfaceType.GetInterfaces().First(i => i.Name == TypesInfo.EnumerableType.Name);

            var elementType = targetInterface.GetGenericArguments().First();

            try
            {
                defaultCollection = Expression.Constant(MethodsInfo.EmptyArray.MakeGenericMethod(elementType).Invoke(null, null));
            }
            catch
            {
                return Expression.Constant(Array.CreateInstance(elementType, 0));
            }
        }

        return defaultCollection;
    }

    /// <summary>
    /// Creates an expression for the default value of a concrete collection type.
    /// </summary>
    internal static Expression CreateConcreteCollectionDefaultValue(Type collectionType)
    {
        try
        {
            if (collectionType.IsArray)
            {
                // For arrays, create an empty array
                var elementType = collectionType.GetElementType();
                return Expression.Constant(Array.CreateInstance(elementType, 0));
            }
            else
            {
                // Try constructor with capacity 0
                var capacityCtor = collectionType.GetConstructor(new[] { typeof(int) });
                if (capacityCtor != null)
                {
                    return Expression.Constant(capacityCtor.Invoke(new object[] { 0 }));
                }
                else
                {
                    // If it doesn't have a constructor with capacity, use parameterless
                    return Expression.Constant(Activator.CreateInstance(collectionType));
                }
            }
        }
        catch { }

        // In case of problems, use default
        return Expression.Default(collectionType);
    }

    /// <summary>
    /// Creates an expression for the default value of a class type.
    /// </summary>
    internal static Expression CreateClassDefaultValue(Type propertyType)
    {
        try
        {
            // If it's a reference type, we need to use the EntityManagerNew class recursively
            // Since this function is only used for required properties, there should be no cyclic dependency

            // Get the Empty method from EntityManagerNew<propertyType> to create an empty instance
            var defaultInstance = typeof(EntityManager<>).MakeGenericType(propertyType)!.GetProperty("Empty")!.GetMethod!;
            // Create an expression for calling this method
            return Expression.Call(null, defaultInstance);
        }
        catch (Exception e)
        {
            // In case of problems, print the error and use default
            Console.WriteLine(e.Message);
            return Expression.Default(propertyType);
        }
    }

    /// <summary>
    /// Creates an expression for the default value of other types.
    /// </summary>
    internal static Expression CreateOtherDefaultValue(Type propertyType)
    {
        if (propertyType.IsInterface)
        {
            try
            {
                // For interfaces, look for an implementation with a parameterless constructor
                List<Type> targetTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(t => !t.IsInterface && !t.IsAbstract && propertyType.IsAssignableFrom(t))
                    .ToList();

                if (targetTypes.Any())
                {
                    var targetType = targetTypes.FirstOrDefault(t => t.GetConstructor(Type.EmptyTypes) != null);
                    if (targetType != null)
                    {
                        return Expression.Constant(Activator.CreateInstance(targetType));
                    }
                }
            }
            catch { }
        }

        // Fallback to default
        return Expression.Default(propertyType);
    }

}
