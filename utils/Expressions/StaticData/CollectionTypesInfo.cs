using Utils.Collections.Collections;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace Utils.Expressions.StaticData;

public static class CollectionTypesInfo
{
    // Usable collection interfaces for the variable data type
    private static readonly HashSet<Type> _usableSystemCollectionInterfaces = new HashSet<Type>
    {
        // Basic collection interfaces
        typeof(IEnumerable),
        typeof(IEnumerable<>),
        typeof(ICollection),
        typeof(ICollection<>),
        
        // Lists
        typeof(IList),
        typeof(IList<>),
        typeof(IReadOnlyCollection<>),
        typeof(IReadOnlyList<>),
        
        // Dictionaries
        typeof(IDictionary),
        typeof(IDictionary<,>),
        typeof(IReadOnlyDictionary<,>),
        
        // Sets
        typeof(ISet<>),
        typeof(IReadOnlySet<>),
        
        // Immutable collections
        typeof(IImmutableList<>),
        typeof(IImmutableSet<>),
        typeof(IImmutableQueue<>),
        typeof(IImmutableStack<>),
        typeof(IImmutableDictionary<,>),
        
        // Other special collections
        typeof(ILookup<,>),
        typeof(IGrouping<,>),
        typeof(IOrderedEnumerable<>),
        typeof(IQueryable),
        typeof(IQueryable<>),
        
        // Collections for concurrent access
        typeof(IProducerConsumerCollection<>),
        
    };

    // Usable collection implementations for the variable data type
    private static readonly HashSet<Type> _usableSystemCollectionImplementations = new HashSet<Type>
    {
        // Basic generic collections
        typeof(List<>),
        typeof(Collection<>),
        typeof(ReadOnlyCollection<>),
        typeof(LinkedList<>),
        typeof(Queue<>),
        typeof(Stack<>),
        
        // Dictionaries and hash tables
        typeof(Dictionary<,>),
        typeof(SortedDictionary<,>),
        typeof(SortedList<,>),
        
        // Sets
        typeof(HashSet<>),
        typeof(SortedSet<>),
        
        // Concurrent collections
        typeof(ConcurrentBag<>),
        typeof(ConcurrentDictionary<,>),
        typeof(ConcurrentQueue<>),
        typeof(ConcurrentStack<>),
        
        // Immutable collections
        typeof(ImmutableArray<>),
        typeof(ImmutableList<>),
        typeof(ImmutableHashSet<>),
        typeof(ImmutableSortedSet<>),
        typeof(ImmutableDictionary<,>),
        typeof(ImmutableSortedDictionary<,>),
        typeof(ImmutableQueue<>),
        typeof(ImmutableStack<>),
        
        // Observable collections
        typeof(ObservableCollection<>),
        typeof(ReadOnlyObservableCollection<>),
        
        // KeyedCollection
        typeof(KeyedCollection<,>),
        
        // Specialized collections
        typeof(BitArray),
        typeof(ListDictionary),
        typeof(OrderedDictionary),
        typeof(StringCollection),
        typeof(StringDictionary),
        typeof(NameValueCollection),
        typeof(HybridDictionary),
        
        // Older (legacy) collections
        typeof(ArrayList),
        typeof(Hashtable),
        typeof(Queue),
        typeof(Stack),
        typeof(SortedList),
        
        // Collections for memory operations
        typeof(Memory<>),
        typeof(ReadOnlyMemory<>)
    };

    // Mapping from interface to their simplest implementations
    private static readonly Dictionary<Type, Type> _interfaceToImplementationMap = new Dictionary<Type, Type>
    {
        // Basic collection interfaces
        { typeof(IEnumerable), typeof(ArrayList) },  // For non-generic
        { typeof(IEnumerable<>), typeof(List<>) },   // For generic
        { typeof(ICollection), typeof(ArrayList) },  // For non-generic
        { typeof(ICollection<>), typeof(List<>) },   // For generic
        
        // Lists
        { typeof(IList), typeof(ArrayList) },        // For non-generic
        { typeof(IList<>), typeof(List<>) },         // For generic
        { typeof(IReadOnlyCollection<>), typeof(ReadOnlyList<>) },
        { typeof(IReadOnlyList<>), typeof(ReadOnlyList<>) },
        
        // Dictionaries
        { typeof(IDictionary), typeof(Hashtable) },  // For non-generic
        { typeof(IDictionary<,>), typeof(Dictionary<,>) },  // For generic
        { typeof(IReadOnlyDictionary<,>), typeof(Utils.Collections.Collections.ReadOnlyDict<,>) },
        
        // Sets
        { typeof(ISet<>), typeof(HashSet<>) },
        { typeof(IReadOnlySet<>), typeof(ImmutableHashSet<>) },  // Not an ideal ReadOnlySet implementation in Core
        
        // Immutable collections
        { typeof(IImmutableList<>), typeof(ImmutableList<>) },
        { typeof(IImmutableSet<>), typeof(ImmutableHashSet<>) },
        { typeof(IImmutableQueue<>), typeof(ImmutableQueue<>) },
        { typeof(IImmutableStack<>), typeof(ImmutableStack<>) },
        { typeof(IImmutableDictionary<,>), typeof(ImmutableDictionary<,>) },
        
        // Other special collections
        { typeof(ILookup<,>), typeof(Utils.Collections.Collections.GroupTable<,>) },    // Typically created via LINQ
        { typeof(IGrouping<,>), typeof(Grouping<,>) },  // Typically created via LINQ
        { typeof(IOrderedEnumerable<>), typeof(OrderedEnumerable<>) },  // Typically created via LINQ
        { typeof(IQueryable), typeof(EnumerableQuery) },
        { typeof(IQueryable<>), typeof(EnumerableQuery<>) },

        // Collections for concurrent access
        { typeof(IProducerConsumerCollection<>), typeof(ConcurrentQueue<>) },

    };

    /// <summary>
    /// Determines whether the type implements IEnumerable and is therefore a collection
    /// </summary>
    public static bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
            return false; // String is a special case - it implements IEnumerable, but we often don't want to treat it as a collection

        return typeof(IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Determines whether the type implements generic IEnumerable<T>
    /// </summary>
    public static bool IsGenericCollectionType(Type type)
    {
        if (type == typeof(string))
            return false;

        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    /// <summary>
    /// Determines whether it is a system collection interface from .NET
    /// </summary>
    public static bool IsSystemCollectionInterface(Type type)
    {
        if (!type.IsInterface)
            return false;

        if (_usableSystemCollectionInterfaces.Contains(type))
            return true;

        // Check generic types
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return _usableSystemCollectionInterfaces.Contains(genericTypeDef);
        }

        return false;
    }

    /// <summary>
    /// Determines whether it is a system collection implementation from .NET
    /// </summary>
    public static bool IsSystemCollectionImplementation(Type type)
    {
        if (type.IsInterface || type.IsAbstract)
            return false;

        // Arrays are a special case
        if (type.IsArray)
            return true;

        // Direct check for known types
        if (_usableSystemCollectionImplementations.Contains(type))
            return true;

        // Check for generic types
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return _usableSystemCollectionImplementations.Contains(genericTypeDef);
        }

        // Check for types in namespaces System.*
        string ns = type.Namespace ?? string.Empty;
        if (ns.StartsWith("System.Collections") && typeof(IEnumerable).IsAssignableFrom(type))
            return true;

        return false;
    }

    /// <summary>
    /// Determines whether it is a custom (non-system) collection implementation
    /// </summary>
    public static bool IsCustomCollectionType(Type type)
    {
        return IsCollectionType(type) &&
               !IsSystemCollectionInterface(type) &&
               !IsSystemCollectionImplementation(type);
    }

    /// <summary>
    /// Returns the simplest implementation for the given collection interface
    /// </summary>
    /// <param name="interfaceType">Interface type</param>
    /// <returns>Implementation type or null if it cannot be determined</returns>
    public static Type? GetDefaultImplementationForInterface(Type interfaceType)
    {
        if (!interfaceType.IsInterface)
            return null;

        // Direct check in mapping
        if (_interfaceToImplementationMap.ContainsKey(interfaceType))
        {
            return _interfaceToImplementationMap[interfaceType];
        }

        // For generic types, check their generic definition
        if (interfaceType.IsGenericType)
        {
            var genericTypeDef = interfaceType.GetGenericTypeDefinition();
            if (_interfaceToImplementationMap.ContainsKey(genericTypeDef))
            {
                var genericImplementationType = _interfaceToImplementationMap[genericTypeDef];

                // Otherwise, create a specific type with generic parameters
                return genericImplementationType.MakeGenericType(interfaceType.GetGenericArguments());
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a default instance for the given collection interface
    /// </summary>
    /// <param name="interfaceType">Interface type</param>
    /// <returns>Instance of the implementation or null if it cannot be created</returns>
    public static object? CreateDefaultImplementationForInterface(Type interfaceType)
    {
        var implementationType = GetDefaultImplementationForInterface(interfaceType);

        if (implementationType is null)
            return null;

        try
        {
            // Try to create an instance
            return Activator.CreateInstance(implementationType);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a default instance of a collection with the given element type
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection</typeparam>
    /// <returns>Instance of the collection or null if it cannot be created</returns>
    public static ICollection<T> CreateDefaultCollection<T>()
    {
        try
        {
            return new List<T>();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Creates a default instance of a dictionary with the given key and value types
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <returns>Instance of the dictionary or null if it cannot be created</returns>
    public static IDictionary<TKey, TValue>? CreateDefaultDictionary<TKey, TValue>() where TKey : notnull
    {
        try
        {
            return new Dictionary<TKey, TValue>();
        }
        catch
        {
            return null;
        }
    }
}
