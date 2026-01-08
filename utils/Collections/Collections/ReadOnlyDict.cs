using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Utils.Collections.Collections
{
    public class ReadOnlyDict<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly IDictionary<TKey, TValue> _innerDictionary;
        public TValue this[TKey key] => _innerDictionary[key];

        public IEnumerable<TKey> Keys => _innerDictionary.Keys;

        public IEnumerable<TValue> Values => _innerDictionary.Values;

        public int Count => _innerDictionary.Count;

        public ReadOnlyDict()
        {
            _innerDictionary = new Dictionary<TKey, TValue>();
        }

        public ReadOnlyDict(IDictionary<TKey, TValue> dictionary)
        {
            _innerDictionary = dictionary.ToDictionary();
        }

        public ReadOnlyDict(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            if (collection is not null)
            {
                _innerDictionary = collection.ToDictionary();
            }
            else
            {
                _innerDictionary = new Dictionary<TKey, TValue>();
            }
        }

        public bool ContainsKey(TKey key) => _innerDictionary.ContainsKey(key);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _innerDictionary.GetEnumerator();

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _innerDictionary.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
