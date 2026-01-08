using System.Collections;
using System.Runtime;

namespace IvoEngine.Expressions.Helpers;



public static class CollectionHelper
{
    /// <summary>
    /// Checks whether both collections are empty or have different sizes.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collections</typeparam>
    /// <param name="leftCollection">First compared collection</param>
    /// <param name="rightCollection">Second compared collection</param>
    /// <param name="equals">Output parameter indicating whether the collections are equal (in case of null or empty collections)</param>
    /// <returns>True if both collections are null, empty, or have different sizes, otherwise False</returns>
    private static bool EmptyOrSizeNotEqual<T>(IEnumerable<T> leftCollection, IEnumerable<T> rightCollection, out bool equals)
    {
        var leftIsNull = leftCollection is null;
        var rightIsNull = rightCollection is null;

        if (leftIsNull && rightIsNull)
        {
            equals = true;

            return true;
        }
        else if (leftIsNull || rightIsNull)
        {
            equals = false;

            return true;
        }

        var leftCollectionEmpty = !leftCollection.Any();
        var rightCollectionEmpty = !rightCollection.Any();

        if (leftCollectionEmpty && rightCollectionEmpty)
        {
            equals = true;

            return true;
        }
        else if (leftCollectionEmpty || rightCollectionEmpty)
        {
            equals = false;

            return true;
        }

        if(leftCollection.Count() != rightCollection.Count())
        {
            equals = false;

            return true;
        }

        equals = false;

        return false;
    }

    /// <summary>
    /// Creates a lookup structure for a collection of elements using the specified comparer.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection</typeparam>
    /// <param name="collection">Collection of elements to create the lookup structure for</param>
    /// <param name="comparer">Comparer for comparing elements</param>
    /// <returns>List containing elements and their duplicates in the collection</returns>
    private static List<Tuple<T, List<T>>> CreateLookup<T>(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        var lookup = new List<Tuple<T, List<T>>> { };

        foreach (var item in collection)
        {
            var targetItem = lookup.FirstOrDefault(x => comparer.Equals(item, x.Item1));

            if (targetItem is null)
            {
                lookup.Add(new Tuple<T, List<T>>(item, new List<T> { item }));
            }
            else
            {
                targetItem.Item2.Add(item);
            }
        }

        return lookup;
    }

    /// <summary>
    /// Compares two dictionaries based on their keys and values.
    /// </summary>
    /// <typeparam name="TKey">Type of dictionary keys</typeparam>
    /// <typeparam name="TValue">Type of dictionary values</typeparam>
    /// <param name="leftCollection">First compared dictionary</param>
    /// <param name="rightCollection">Second compared dictionary</param>
    /// <param name="comparer">Comparer for comparing dictionary values</param>
    /// <returns>True if the dictionaries contain the same key-value pairs, otherwise False</returns>
    public static bool DictionariesEquals<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey,TValue>> leftCollection, 
        IEnumerable<KeyValuePair<TKey, TValue>> rightCollection,
        IEqualityComparer<TValue> comparer) where TKey : notnull
    {
        if (comparer is null)
        {
            comparer = EqualityComparer<TValue>.Default;
        }

        if(EmptyOrSizeNotEqual(leftCollection, rightCollection, out var equals))
        {
            return equals;
        }

        var leftDict = leftCollection.ToDictionary(x => x.Key, x => x.Value);
        var rightDict = rightCollection.ToDictionary(x => x.Key, x => x.Value);

        foreach(var item in leftDict)
        {
            var key = item.Key;
            var leftValue = item.Value;

            if (rightDict.TryGetValue(key, out var rightValue))
            {
                if(!comparer.Equals(leftValue,rightValue))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        
        return true;
    }


    /// <summary>
    /// Compares two collections regardless of the order of elements.
    /// Takes into account the number of occurrences of each element in the collections.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collections</typeparam>
    /// <param name="leftCollection">First compared collection</param>
    /// <param name="rightCollection">Second compared collection</param>
    /// <param name="comparer">Comparer for comparing elements (optional)</param>
    /// <returns>True if the collections contain the same elements with the same frequencies, otherwise False</returns>
    public static bool CollectionEquals<T>(IEnumerable<T> leftCollection, IEnumerable<T> rightCollection, IEqualityComparer<T>? comparer = null)
    {
        if(comparer is null)
        {
            comparer = EqualityComparer<T>.Default;
        }

        if (EmptyOrSizeNotEqual(leftCollection, rightCollection, out var equals))
        {
            return equals;
        }

        var leftLookup = CreateLookup(leftCollection, comparer);
        var rightLookup = CreateLookup(rightCollection, comparer);

        if(leftLookup.Count != rightLookup.Count)
        {
            return false;
        }

        foreach(var item in leftLookup)
        {
            var targetItem = rightLookup.FirstOrDefault(x => comparer.Equals(x.Item1, item.Item1));

            if(targetItem is null)
            {
                return false;
            }

            if(item.Item2.Count != targetItem.Item2.Count)
            {
                return false;
            }    
        }

        return true;
    }

    /// <summary>
    /// Compares two sequences of elements with respect to their order.
    /// </summary>
    /// <typeparam name="T">Type of elements in the sequences</typeparam>
    /// <param name="leftCollection">First compared sequence</param>
    /// <param name="rightCollection">Second compared sequence</param>
    /// <param name="comparer">Comparer for comparing elements (optional)</param>
    /// <returns>True if the sequences contain the same elements in the same order, otherwise False</returns>
    public static bool SequenceEquals<T>(IEnumerable<T> leftCollection, IEnumerable<T> rightCollection, IEqualityComparer<T>? comparer= null)
    {
        if (comparer is null)
        {
            comparer = EqualityComparer<T>.Default;
        }

        if (EmptyOrSizeNotEqual(leftCollection, rightCollection, out var equals))
        {
            return equals;
        }

        var leftList = leftCollection.ToList();
        var rightList = rightCollection.ToList();

        for (int i = 0; i < leftList.Count; i++)
        {
            if (!comparer.Equals(leftList[i], rightList[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static IEnumerable<T>? UpdateCollection<T>(IEnumerable<T> targetCollection, IEnumerable<T> sourceCollection)
    {
        if (sourceCollection is null)
        {
            return null;
        }

        if (!sourceCollection.Any())
        {
            return [];
        }

        if (targetCollection is ICollection<T> collection)
        {
            collection.Clear();

            var enumerator = sourceCollection.GetEnumerator();

            while (enumerator.MoveNext())
            {
                collection.Add(enumerator.Current);
            }

            return collection;
        }
        else
        {
            return sourceCollection.ToArray();
        }
    }

    public static IEnumerable<TOut> CollectionConversion<TIn, TOut>(IEnumerable<TIn> sourceCollection, Func<TIn, TOut> conversionFunction)
    {
        return sourceCollection.Select(x =>  conversionFunction(x));
    }

    public static KeyValuePair<TKeyOut, TValueOut> ConvertKeyValuePair<TKeyIn, TValueIn, TKeyOut, TValueOut>(KeyValuePair<TKeyIn, TValueIn> sourceKvp, Func<TKeyIn,TKeyOut> keyConversion, Func<TValueIn, TValueOut> valueConversion)
    {
        return new KeyValuePair<TKeyOut, TValueOut>(keyConversion(sourceKvp.Key), valueConversion(sourceKvp.Value));
    }
}
