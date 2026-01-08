using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Utils.Extensions;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using Utils.Expressions.StaticData;

namespace Utils.Expressions.Helpers;

/// <summary>
/// Provides helper methods for working with types during entity mapping.
/// </summary>
public static class TypeHelper
{
    /// <summary>
    /// Determines whether the type is nullable, and if so, gets its underlying type.
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <param name="underlyingType">Output parameter containing the underlying type if the input type is nullable</param>
    /// <returns>True if the type is nullable, otherwise False</returns>
    public static bool IsNullable(Type type, out Type? underlyingType)
    {
        return IsNullableInternal(type, out underlyingType);
    }

    /// <summary>
    /// Determines whether the property is marked as nullable using attributes.
    /// </summary>
    /// <param name="propertyInfo">PropertyInfo to analyze</param>
    /// <returns>True if the property is marked as nullable, otherwise False</returns>
    public static bool IsMarkedAsNullable(PropertyInfo propertyInfo)
    {
        return IsMarkedAsNullableInternal(propertyInfo);
    }

    /// <summary>
    /// Determines whether the given data type represents a .NET system data type
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <returns>True if the data type is a collection, otherwise False</returns>
    public static bool IsPrimitiveType(Type type)
    {
        return IsPrimitiveTypeInternal(type);
    }

    public static bool IsKeyValuePair(Type type)
    {
        if (type.Name == TypesInfo.KeyValuePairType.Name)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether the given data type represents a collection of elements (IEnumerable interface)
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <returns>True if the data type is a collection, otherwise False</returns>
    public static bool IsCollectionType(Type type)
    {
        return IsCollectionTypeInternal(type);
    }

    /// <summary>
    /// Determines whether the given data type represents a collection defined by a .NET system type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSystemCollectionInterface(Type type)
    {
        return IsSystemCollectionTypeInternal(type);
    }

    /// <summary>
    /// Determines whether the given data type is a concrete implementation of the specified abstract type and can be instantiated
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <param name="abstractType">Abstract data type</param>
    /// <returns>True if the data type is an instantiable descendant of the abstract type, otherwise False</returns>
    public static bool IsConcreteImplementation(Type type, Type abstractType)
    {
        if (type.IsInterface || type.IsAbstract)
        {
            return false;
        }

        return abstractType.IsAssignableFrom(type);
    }

    public static bool IsRecord(Type type)
    {
        // Records have a special attribute or inherit from a base type
        return type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) != null ||
               type.GetCustomAttributes().Any(attr => attr.GetType().Name == "CompilerGeneratedAttribute");
    }

    /// <summary>
    /// Gets the target type for generic parameter T.
    /// If T is a nullable type, returns its underlying type.
    /// </summary>
    /// <typeparam name="T">Generic parameter</typeparam>
    /// <returns>Target type for work in EntityMapper</returns>
    public static Type GetTargetType<T>()
    {
        Type type;
        var originalType = typeof(T);

        // If the type is nullable, get its underlying type
        if (IsNullable(originalType, out var underlyingType))
        {
            type = underlyingType ?? typeof(void);
        }
        else
        {
            // Otherwise use the original type
            type = originalType;
        }

        return type;
    }

    /// <summary>
    /// Gets the element type of a collection.
    /// </summary>
    /// <param name="collectionType">Collection type</param>
    /// <returns>Element type of the collection or null if it cannot be determined</returns>
    public static Type? GetElementType(Type collectionType)
    {
        return GetElementTypeInternal(collectionType);
    }



    /// <summary>
    /// Checks whether the given data type is a descendant of another type
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <param name="ancestorType">Data type against which we examine inheritance</param>
    /// <returns>True if the data type is a descendant of the specified type, otherwise False</returns>
    private static bool IsDescendantType(Type type, Type ancestorType)
    {
        if (type == ancestorType)
        {
            return false;
        }

        Type? baseType = type.BaseType;

        while (baseType is not null)
        {
            if (baseType == ancestorType)
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Sorts types so that ancestors come before their descendants using the Sort method
    /// </summary>
    public static IEnumerable<Type> SortByInheritance(IEnumerable<Type> types)
    {
        if (types == null)
            throw new ArgumentNullException(nameof(types));

        // Create a list of types so we can use the Sort method
        var typeList = types.ToList();

        // Use a custom comparison method
        typeList.Sort((x, y) =>
        {
            if (x == y) return 0;

            // If y is an ancestor of x, then x should be later (greater)
            else if (IsDescendantType(x, y)) return 1;

            // If x is an ancestor of y, then x should be earlier (smaller)
            else if (IsDescendantType(y, x)) return -1;

            // Types are not in the inheritance hierarchy, keep their original order
            else return 0;
        });

        return typeList;
    }

    #region Internal Implementation

    /// <summary>
    /// Determines whether the given data type represents a .NET system data type
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <returns>True if the data type is a collection, otherwise False</returns>
    public static bool IsPrimitiveTypeInternal(Type type)
    {
        return TypesInfo.PrimitiveTypes.Contains(type);
    }
    /// <summary>
    /// Determines whether the given data type represents a collection of elements (IEnumerable interface)
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <returns>True if the data type is a collection, otherwise False</returns>
    internal static bool IsCollectionTypeInternal(Type type)
    {
        if (type == TypesInfo.StringType)
        {
            return false;
        }
        if (type.IsArray)
        {
            return true;
        }
        else if (TypesInfo.EnumerableInterface.IsAssignableFrom(type))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the given data type represents a collection defined by a .NET system type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static bool IsSystemCollectionTypeInternal(Type type)
    {
        if (type.IsArray)
        {
            return true;
        }
        else if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();

            return TypesInfo.CollectionInterfaces.Contains(genericType);
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether the given collection is of type IDictionary (elements of type KeyValuePair)
    /// </summary>
    /// <param name="collectionType">Data type to analyze</param>
    /// <returns>True if the data type is IDictionary</returns>
    internal static bool IsDictionary(Type collectionType)
    {
        var genericType = collectionType.GetGenericTypeDefinition();

        if (genericType == typeof(IDictionary<,>))
        {
            return true;
        }
        else if (genericType == typeof(IReadOnlyDictionary<,>))
        {
            return true;
        }
        else if (typeof(IDictionary).IsAssignableFrom(collectionType))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the data type of elements of a generic collection (IEnumerable interface)
    /// </summary>
    /// <param name="collectionType"></param>
    /// <returns></returns>
    internal static Type GetGenericElementType(Type collectionType)
    {
        if (IsDictionary(collectionType))
        {
            return typeof(KeyValuePair<,>).MakeGenericType(collectionType.GetGenericArguments());
        }

        return collectionType.GetGenericArguments().First();
    }

    /// <summary>
    /// Gets the element type of a collection.
    /// </summary>
    /// <param name="collectionType">Collection type</param>
    /// <returns>Element type of the collection or null if it cannot be determined</returns>
    internal static Type? GetElementTypeInternal(Type collectionType)
    {
        // For arrays, return the element type directly
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        // For generic collections
        if (collectionType.IsGenericType)
        {
            if (IsCollectionTypeInternal(collectionType))
            {
                return GetGenericElementType(collectionType);
            }
        }

        // For other collections, look for an implementation of IEnumerable<T>
        foreach (var interfaceType in collectionType.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == TypesInfo.EnumerableType)
            {
                return interfaceType.GetGenericArguments().First();
            }
        }

        // If we don't find a generic parameter, return object as the default type
        if (TypesInfo.EnumerableInterface.IsAssignableFrom(collectionType))
        {
            return typeof(object);
        }

        return null;
    }


    /// <summary>
    /// Determines whether the type is nullable, and if so, gets its underlying type.
    /// </summary>
    /// <param name="type">Type to analyze</param>
    /// <param name="underlyingType">Output parameter containing the underlying type if the input type is nullable</param>
    /// <returns>True if the type is nullable, otherwise False</returns>
    internal static bool IsNullableInternal(Type type, out Type? underlyingType)
    {
        // Determines whether the type is generic and whether its generic definition is Nullable<>
        bool flag = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        // If the type is nullable, get its underlying type, otherwise set null
        underlyingType = flag ? Nullable.GetUnderlyingType(type) : null;
        return flag;
    }

    /// <summary>
    /// Determines whether the property is marked as nullable using attributes.
    /// </summary>
    /// <param name="propertyInfo">PropertyInfo to analyze</param>
    /// <returns>True if the property is marked as nullable, otherwise False</returns>
    internal static bool IsMarkedAsNullableInternal(PropertyInfo propertyInfo)
    {
        // Uses NullabilityInfoContext to determine whether the property is marked as nullable
        return new NullabilityInfoContext().Create(propertyInfo).WriteState is NullabilityState.Nullable;
    }

    #endregion
}
