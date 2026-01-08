using Utils.Expressions.Helpers;
using System.Reflection;
using System.Linq;

namespace Utils.Expressions.StaticData;

public static class MethodsInfo
{
    private static readonly Type ArrayType = typeof(Array);
    private static readonly Type EnumerableType = typeof(Enumerable);
    private static readonly Type CollectionComparerType = typeof(CollectionHelper);
    
    private static readonly MethodInfo _emptyArray = ArrayType.GetMethod("Empty")!;
    private static readonly MethodInfo _compareSequence = CollectionComparerType.GetMethod("SequenceEquals")!;
    private static readonly MethodInfo _compareCollection = CollectionComparerType.GetMethod("CollectionEquals")!;
    private static readonly MethodInfo _compareDictionary = CollectionComparerType.GetMethod("DictionariesEquals")!;
    private static readonly MethodInfo _updateCollection = CollectionComparerType.GetMethod("UpdateCollection")!;

    private static readonly MethodInfo _selectMethodDefinition = EnumerableType.GetMethods()
                                                                               .Where(m => m.Name == "Select" && m.IsGenericMethodDefinition)
                                                                               .Where(m => m.GetGenericArguments().Length == 2)
                                                                               .First(m => m.GetParameters().Length == 2);

    private static readonly MethodInfo _toArrayMethodDefinition = EnumerableType.GetMethods()
                                                                                .Where(m => m.Name == "ToArray" && m.IsGenericMethodDefinition)
                                                                                .First();

    private static readonly MethodInfo _toListMethodDefinition = EnumerableType.GetMethods()
                                                                               .Where(m => m.Name == "ToList" && m.IsGenericMethodDefinition)
                                                                               .First();

    private static readonly MethodInfo _whereMethodDefinition = EnumerableType.GetMethods()
                                                                              .Where(m => m.Name == "Where" && m.IsGenericMethodDefinition)
                                                                              .Where(m => m.GetGenericArguments().Length == 1)
                                                                              .First(m => m.GetParameters().Length == 2);

    private static readonly MethodInfo _countMethodDefinition = EnumerableType.GetMethods()
                                                                              .Where(m => m.Name == "Count" && m.IsGenericMethodDefinition)
                                                                              .Where(m => m.GetGenericArguments().Length == 1)
                                                                              .First(m => m.GetParameters().Length == 1);

    private static readonly MethodInfo _firstOrDefaultMethodDefinition = EnumerableType.GetMethods()
                                                                                       .Where(m => m.Name == "FirstOrDefault" && m.IsGenericMethodDefinition)
                                                                                       .Where(m => m.GetGenericArguments().Length == 1)
                                                                                       .First(m => m.GetParameters().Length == 1);
    private static readonly MethodInfo _toDictionaryMethodDefinition = EnumerableType.GetMethods()
                                                                                     .Where(m => m.Name == "ToDictionary" && m.IsGenericMethodDefinition)
                                                                                     .Where(m => m.GetGenericArguments().Length == 2)
                                                                                     .First(m => m.GetParameters().Length == 1);
                           
    // Conversion operators
    private static readonly string _implicitOperatorName = "op_Implicit";
    private static readonly string _explicitOperatorName = "op_Explicit";

    public static MethodInfo EmptyArray => _emptyArray;
    public static MethodInfo CompareSequence => _compareSequence;
    public static MethodInfo CompareCollection => _compareCollection;
    public static MethodInfo CompareDictionary => _compareDictionary;
    public static MethodInfo UpdateCollection => _updateCollection;

    public static MethodInfo SelectMethodDefinition => _selectMethodDefinition;
    public static MethodInfo ToArrayMethodDefinition => _toArrayMethodDefinition;
    public static MethodInfo ToListMethodDefinition => _toListMethodDefinition;
    public static MethodInfo WhereMethodDefinition => _whereMethodDefinition;
    public static MethodInfo CountMethodDefinition => _countMethodDefinition;
    public static MethodInfo ToDictionaryMethodDefinition => _toDictionaryMethodDefinition;
    public static MethodInfo FirstOrDefaultMethodDefinition => _firstOrDefaultMethodDefinition;

    public static string ImplicitOperatorName => _implicitOperatorName;
    public static string ExplicitOperatorName => _explicitOperatorName;

    /// <summary>
    /// Creates an instance of the generic Select method with specific types.
    /// </summary>
    public static MethodInfo MakeSelect<TSource, TResult>()
    {
        return _selectMethodDefinition.MakeGenericMethod(typeof(TSource), typeof(TResult));
    }

    /// <summary>
    /// Creates an instance of the generic Select method with the given types.
    /// </summary>
    public static MethodInfo MakeSelect(Type sourceType, Type resultType)
    {
        return _selectMethodDefinition.MakeGenericMethod(sourceType, resultType);
    }

    /// <summary>
    /// Creates an instance of the generic ToArray method with a specific type.
    /// </summary>
    public static MethodInfo MakeToArray<T>()
    {
        return _toArrayMethodDefinition.MakeGenericMethod(typeof(T));
    }

    /// <summary>
    /// Creates an instance of the generic ToArray method with the given type.
    /// </summary>
    public static MethodInfo MakeToArray(Type elementType)
    {
        return _toArrayMethodDefinition.MakeGenericMethod(elementType);
    }

    /// <summary>
    /// Creates an instance of the generic ToList method with a specific type.
    /// </summary>
    public static MethodInfo MakeToList<T>()
    {
        return _toListMethodDefinition.MakeGenericMethod(typeof(T));
    }

    /// <summary>
    /// Creates an instance of the generic ToList method with the given type.
    /// </summary>
    public static MethodInfo MakeToList(Type elementType)
    {
        return _toListMethodDefinition.MakeGenericMethod(elementType);
    }

    /// <summary>
    /// Creates an instance of the generic Where method with a specific type.
    /// </summary>
    public static MethodInfo MakeWhere<T>()
    {
        return _whereMethodDefinition.MakeGenericMethod(typeof(T));
    }

    /// <summary>
    /// Creates an instance of the generic Where method with the given type.
    /// </summary>
    public static MethodInfo MakeWhere(Type elementType)
    {
        return _whereMethodDefinition.MakeGenericMethod(elementType);
    }

    public static MethodInfo MakeToDictionary(Type keyType, Type valueType)
    {
        return _toDictionaryMethodDefinition.MakeGenericMethod(keyType, valueType);
    }

    /// <summary>
    /// Finds a conversion operator between two types.
    /// </summary>
    public static MethodInfo FindConversionOperator(Type sourceType, Type targetType, string operatorName)
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
        var targetMethods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == operatorName)
            .Where(m => m.ReturnType == targetType)
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == sourceType)
            .FirstOrDefault();

        return targetMethods;
    }

    /// <summary>
    /// Finds an implicit conversion operator between two types.
    /// </summary>
    public static MethodInfo FindImplicitConversionOperator(Type sourceType, Type targetType)
    {
        return FindConversionOperator(sourceType, targetType, _implicitOperatorName);
    }

    /// <summary>
    /// Finds an explicit conversion operator between two types.
    /// </summary>
    public static MethodInfo FindExplicitConversionOperator(Type sourceType, Type targetType)
    {
        return FindConversionOperator(sourceType, targetType, _explicitOperatorName);
    }
}
