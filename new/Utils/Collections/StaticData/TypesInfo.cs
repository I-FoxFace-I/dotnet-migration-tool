using IvoEngine.Collections.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace IvoEngine.Collections.StaticData;

public static class TypesInfo
{
    private static readonly Type _uriType = typeof(Uri);
    private static readonly Type _listType = typeof(List<>);
    private static readonly Type _stringType = typeof(string);
    private static readonly Type _keyValueType = typeof(KeyValuePair<,>);
    private static readonly Type _dictionaryType = typeof(Dictionary<,>);
    private static readonly Type _enumerableType = typeof(IEnumerable<>);
    private static readonly Type _enumerableInterface = typeof(IEnumerable);
    private static readonly Type _equalityComparerType = typeof(EqualityComparer<>);

    /// <summary>
    /// List of basic system types supported in the library.
    /// </summary>
    private static readonly Type[] _primitiveTypes = new[]
    {
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(char),
        typeof(decimal),
        typeof(double),
        typeof(float),
        typeof(int),
        typeof(uint),
        typeof(nint),
        typeof(nuint),
        typeof(long),
        typeof(ulong),
        typeof(short),
        typeof(ushort),
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(Guid),
        typeof(Index),
        typeof(TimeSpan),
        typeof(Version),
        typeof(Type),
        typeof(Uri),
        typeof(TimeOnly),
        typeof(DateOnly),
        typeof(Half),
        typeof(BigInteger),
        typeof(Complex),
        typeof(Range),
        typeof(Rune)
    };

    private static readonly Dictionary<Type, int> _primitiveTypeIndexes = CreateTypeIndexDictionary();

    /// <summary>
    /// List of basic system types supported in the library.
    /// </summary>
    private static Dictionary<Type,string> _primitiveTypeNames = new Dictionary<Type, string>
    {
        { typeof(bool), "bool"},
        { typeof(byte), "byte"},
        { typeof(sbyte), "sbyte"},
        { typeof(char), "char"},
        { typeof(decimal), "decimal"},
        { typeof(double), "double"},
        { typeof(float), "float"},
        { typeof(int), "int"},
        { typeof(uint), "uint"},
        { typeof(nint), "nint"},
        { typeof(nuint), "nuint"},
        { typeof(long), "long"},
        { typeof(ulong), "ulong"},
        { typeof(short), "short"},
        { typeof(ushort), "ushort"},
        { typeof(string), "string"},
        { typeof(DateTime), "DateTime"},
        { typeof(DateTimeOffset), "DateTimeOffset"},
        { typeof(Guid), "Guid"},
        { typeof(Index), "Index"},
        { typeof(TimeSpan), "TimeSpan"},
        { typeof(Version), "Version"},
        { typeof(Type), "Type"},
        { typeof(Uri), "Uri"},
        { typeof(TimeOnly), "TimeOnly"},
        { typeof(DateOnly), "DateOnly"},
        { typeof(Half), "Half"},
        { typeof(BigInteger), "BigInteger"},
        { typeof(Complex), "Complex"},
        { typeof(Range), "Range"},
        { typeof(Rune), "Rune" }
    };


    /// <summary>
    /// List of basic generic interfaces for collections.
    /// </summary>
    private static readonly Type[] _collectionInterfaces = new[]
    {
        typeof(IEnumerable<>),
        typeof(ICollection<>),
        typeof(IReadOnlyCollection<>),
        typeof(IList<>),
        typeof(IReadOnlyList<>),
        typeof(IImmutableSet<>),
        typeof(ISet<>),
        typeof(IReadOnlySet<>),
        typeof(IImmutableSet<>),
        typeof(ILookup<,>),
        typeof(IDictionary<,>),
        typeof(IReadOnlyDictionary<,>),
        typeof(IImmutableDictionary<,>),
        typeof(IImmutableQueue<>),
        typeof(IImmutableStack<>)
    };

    private static readonly Dictionary<Type, Type> _collectionDefaultType = new Dictionary<Type, Type>
    {
        { typeof(IEnumerable<>),typeof(List<>)},
        { typeof(ICollection<>),typeof(List<>)},
        { typeof(IReadOnlyCollection<>),typeof(ReadOnlyCollection<>)},
        { typeof(IList<>),typeof(List<>)},
        { typeof(IReadOnlyList<>),typeof(ReadOnlyList<>)},
        { typeof(IImmutableList<>),typeof(ImmutableList<>)},
        { typeof(ISet<>),typeof(HashSet<>)},
        { typeof(IReadOnlySet<>),typeof(IvoEngine.Collections.Collections.ReadOnlySet<>)},
        { typeof(IImmutableSet<>),typeof(ImmutableHashSet<>)},
        { typeof(ILookup<,>),typeof(GroupTable<,>)},
        { typeof(IDictionary<,>),typeof(Dictionary<,>)},
        { typeof(IReadOnlyDictionary<,>),typeof(ReadOnlyDict<,>)},
        { typeof(IImmutableDictionary<,>),typeof(ImmutableDictionary<,>)},
        { typeof(IImmutableQueue<>),typeof(ImmutableQueue<>)},
        { typeof(IImmutableStack<>),typeof(ImmutableStack<>)},
    };

    private static Dictionary<Type, int> CreateTypeIndexDictionary()
    {
        Dictionary<Type, int> typeIndexDictionary = new Dictionary<Type, int>();
        
        for (int i = 0; i < _primitiveTypes.Length; i++)
        {
            typeIndexDictionary.Add(_primitiveTypes[i], i);
        }

        return typeIndexDictionary;
    }
    public static Type UriType => _uriType;
    public static Type ListType => _listType;
    public static Type StringType => _stringType;
    public static Type EnumerableType => _enumerableType;
    public static Type DictionaryType => _dictionaryType;
    public static Type KeyValuePairType => _keyValueType;
    public static Type EnumerableInterface => _enumerableInterface;
    public static Type EqualityComparerType => _equalityComparerType;
    public static Type[] PrimitiveTypes => _primitiveTypes;
    public static Type[] CollectionInterfaces => _collectionInterfaces;
    public static Dictionary<Type, int> PrimitiveTypeIndexes => _primitiveTypeIndexes;
    public static Dictionary<Type, Type> CollectionDefaultTypes => _collectionDefaultType;

    /// <summary>
    /// Determines whether the type is nullable, and if so, gets its underlying type.
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <param name="underlyingType">Output parameter containing the underlying type if the input type is nullable</param>
    /// <returns>True if the type is nullable, otherwise False</returns>
    public static bool IsNullable(Type type, out Type? underlyingType)
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
    public static bool IsMarkedAsNullable(PropertyInfo propertyInfo)
    {
        return new NullabilityInfoContext().Create(propertyInfo).WriteState is NullabilityState.Nullable;
    }

    /// <summary>
    /// Determines whether the given data type represents a .NET system data type
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <returns>True if the data type is a collection, otherwise False</returns>
    public static bool IsPrimitiveType(Type type)
    {
        return _primitiveTypes.Contains(type);
    }


    /// <summary>
    /// Determines whether the given data type represents a collection of elements (IEnumerable interface)
    /// </summary>
    /// <param name="type">Data type to analyze</param>
    /// <returns>True if the data type is a collection, otherwise False</returns>
    public static bool IsCollectionType(Type type)
    {
        if (type == _stringType)
        {
            return false;
        }
        if (type.IsArray)
        {
            return true;
        }
        else if (_enumerableInterface.IsAssignableFrom(type))
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
    public static bool IsCollectionInterface(Type type)
    {
        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();

            return _collectionInterfaces.Contains(genericType);
        }
        else
        {
            return false;
        }
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
    /// Gets the element type of a collection.
    /// </summary>
    /// <param name="collectionType">Collection type</param>
    /// <returns>Element type of the collection or null if it cannot be determined</returns>
    public static Type? GetElementType(Type collectionType)
    {
        // For arrays, return the element type directly
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        // For generic collections
        if (collectionType.IsGenericType)
        {
            if (IsCollectionType(collectionType))
            {
                return GetGenericElementType(collectionType);
            }
        }

        // For other collections, look for an implementation of IEnumerable<T>
        foreach (var interfaceType in collectionType.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == _enumerableType)
            {
                return interfaceType.GetGenericArguments().First();
            }
        }

        // If we don't find a generic parameter, return object as the default type
        if (collectionType != _stringType && _enumerableInterface.IsAssignableFrom(collectionType))
        {
            return typeof(object);
        }

        return null;
    }



    /// <summary>
    /// Determines whether the given collection is of type IDictionary (elements of type KeyValuePair)
    /// </summary>
    /// <param name="collectionType">Data type to analyze</param>
    /// <returns>True if the data type is IDictionary</returns>
    public static bool IsDictionary(Type collectionType)
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
    public static Type GetGenericElementType(Type collectionType)
    {
        if (IsDictionary(collectionType))
        {
            return typeof(KeyValuePair<,>).MakeGenericType(collectionType.GetGenericArguments());
        }

        return collectionType.GetGenericArguments().First();
    }

    public static string GetGenericTypeName(Type type)
    {
        var fullName = type.Name;
        // Remove possible type parameters from the name (e.g. "List`1" -> "List")
        var backtickIndex = fullName.IndexOf('`');
        if (backtickIndex > 0)
        {
            return fullName.Substring(0, backtickIndex);
        }
        return fullName;
    }

    public static string GetPrimitiveTypeName(Type type)
    {
        if(_primitiveTypeNames.TryGetValue(type, out var typeName))
        {
            return typeName;
        }

        return type.Name;
    }
}
