using IvoEngine.Expressions.Helpers;
using IvoEngine.Expressions.StaticData;
using System.Linq.Expressions;

namespace IvoEngine.Expressions.ExpressionUtils;

public static class MethodUtils
{
    private static Expression CreateComparer(Type elementType)
    {
        if (TypeHelper.IsPrimitiveType(elementType))
        {
            return TypeUtils.DefaultComparer(elementType);
        }
        else
        {
            return TypeUtils.UniversalComparer(elementType);
        }
    }

    private static Type GetEnumerableInterface(Type collectionType)
    {
        if (collectionType.Name == TypesInfo.EnumerableType.Name)
        {
            return collectionType;
        }
        else
        {
            return collectionType.GetInterfaces().First(i => i.Name == TypesInfo.EnumerableType.Name);
        }
    }

    public static Expression CompareCollections(Type collectionType, Expression leftCollection, Expression rightCollection)
    {
        var targetInterface = GetEnumerableInterface(collectionType);

        var elementType = targetInterface.GetGenericArguments().First();

        if (elementType.Name == TypesInfo.KeyValuePairType.Name)
        {
            var keyType = elementType.GetGenericArguments().ElementAt(0);
            var valueType = elementType.GetGenericArguments().ElementAt(1);
            var compareMethod = MethodsInfo.CompareDictionary.MakeGenericMethod(keyType, valueType);

            return Expression.Call(null, compareMethod, leftCollection, rightCollection, CreateComparer(valueType));
        }
        else
        {
            var compareMethod = MethodsInfo.CompareCollection.MakeGenericMethod(elementType);

            return Expression.Call(null, compareMethod, leftCollection, rightCollection, CreateComparer(elementType));
        }
    }

    public static Expression UpdateCollection(Type collectionType, Expression targetCollection, Expression sourceCollection, bool nullableProperty, Expression defaultValue)
    {   
        var targetInterface = GetEnumerableInterface(collectionType);

        var elementType = targetInterface.GetGenericArguments().First();

        // Check if the source collection is null
        var nullSource = Expression.Equal(sourceCollection, Expression.Constant(null));

        if (collectionType.IsInterface)
        {
            var genericInterface = collectionType.GetGenericTypeDefinition();
            if (TypeHelper.IsSystemCollectionInterface(genericInterface))
            {
                if (elementType.Name == TypesInfo.KeyValuePairType.Name)
                {
                    var keyType = elementType.GetGenericArguments().ElementAt(0);
                    var valueType = elementType.GetGenericArguments().ElementAt(1);
                    collectionType = TypesInfo.CollectionDefaultTypes[genericInterface].MakeGenericType(keyType, valueType);
                }
                else
                {
                    collectionType = TypesInfo.CollectionDefaultTypes[genericInterface].MakeGenericType(elementType);
                }
            }
        }
        
        if(collectionType.IsArray)
        {
            var toArrayMethod = MethodsInfo.MakeToArray(elementType);
            var collectionUpdate = Expression.Call(null, toArrayMethod, sourceCollection);

            return Expression.IfThenElse(
                    nullSource,
                    Expression.Assign(targetCollection, defaultValue),
                    Expression.Assign(targetCollection, Expression.TypeAs(collectionUpdate, collectionType))
                );

        }
        if (ConstucotrHelper.HasCollectionConstructor(collectionType, out var constructorInfo))
        {
            var collectionUpdate = Expression.New(constructorInfo!, sourceCollection);

            return Expression.IfThenElse(
                    nullSource,
                    Expression.Assign(targetCollection, defaultValue),
                    Expression.Assign(targetCollection, Expression.TypeAs(collectionUpdate, collectionType))
                );
        }
        else
        {
            var emptyArray = DefaultValueUtils.DefaultValue(collectionType);
            var updateMethod = MethodsInfo.UpdateCollection.MakeGenericMethod(elementType);
            var collectionUpdate = Expression.Call(null, updateMethod, emptyArray, sourceCollection);

            return Expression.IfThenElse(
                nullSource,
                Expression.Assign(targetCollection, defaultValue),
                Expression.Assign(targetCollection, Expression.TypeAs(collectionUpdate, collectionType))
            );
        }
    }
}
