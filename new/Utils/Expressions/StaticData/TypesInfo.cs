using IvoEngine.Collections.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using IvoEngine.Expressions;

namespace IvoEngine.Expressions.StaticData;

public static class TypesInfo
{
    private static readonly Type _uriType = typeof(Uri);
    private static readonly Type _listType = typeof(List<>);
    private static readonly Type _stringType = typeof(string);
    private static readonly Type _keyValueType = typeof(KeyValuePair<,>);
    private static readonly Type _dictionaryType = typeof(Dictionary<,>);
    private static readonly Type _enumerableType = typeof(IEnumerable<>);
    private static readonly Type _enumerableInterface = typeof(IEnumerable);

    private static readonly Type _entityManagerType = typeof(EntityManager<>);
    private static readonly Type _equalityComparerType = typeof(EqualityComparer<>);
    private static readonly Type _universalComparerType = typeof(UniversalComparer<>);

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
    public static Type EntityManagerType => _entityManagerType;
    public static Type EnumerableInterface => _enumerableInterface;
    public static Type EqualityComparerType => _equalityComparerType;
    public static Type UniversalComparerType => _universalComparerType;
    public static Type[] PrimitiveTypes => _primitiveTypes;
    public static Type[] CollectionInterfaces => _collectionInterfaces;
    public static Dictionary<Type, int> PrimitiveTypeIndexes => _primitiveTypeIndexes;
    public static Dictionary<Type, Type> CollectionDefaultTypes => _collectionDefaultType;
}
