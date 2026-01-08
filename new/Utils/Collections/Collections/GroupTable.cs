using System.Collections;

namespace IvoEngine.Collections.Collections
{

    /// <summary>
    /// Implementace Lookup, do které lze i vkládat nové hodnoty.
    /// Tato struktura mapuje klíče na skupiny hodnot a umožňuje přidávání, odebírání a vyhledávání.
    /// </summary>
    /// <typeparam name="TKey">Typ klíče</typeparam>
    /// <typeparam name="TValue">Typ hodnoty</typeparam>
    public class GroupTable<TKey, TValue> : ILookup<TKey, TValue> where TKey : notnull
    {
        // Vnitřní slovník, kde každý klíč mapuje na skupinu hodnot
        private readonly Dictionary<TKey, List<TValue>> _dictionary;

        // Komparátor klíčů
        private readonly IEqualityComparer<TKey> _comparer;

        /// <summary>
        /// Počet unikátních klíčů v lookup struktuře.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Vytvoří novou instanci MutableLookup.
        /// </summary>
        public GroupTable()
        {
            _comparer = EqualityComparer<TKey>.Default;
            _dictionary = new Dictionary<TKey, List<TValue>>(_comparer);
        }

        /// <summary>
        /// Vytvoří novou instanci MutableLookup s daným komparátorem klíčů.
        /// </summary>
        /// <param name="comparer">Komparátor pro porovnávání klíčů</param>
        public GroupTable(IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _dictionary = new Dictionary<TKey, List<TValue>>(_comparer);
        }

        /// <summary>
        /// Vytvoří novou instanci MutableLookup s daným komparátorem klíčů.
        /// </summary>
        /// <param name="comparer">Komparátor pro porovnávání klíčů</param>
        public GroupTable(ILookup<TKey, TValue> values)
        {
            _comparer = EqualityComparer<TKey>.Default;
            _dictionary = new Dictionary<TKey, List<TValue>>(_comparer);
            
            foreach (var item in values)
            {
                _dictionary.Add(item.Key, item.ToList());
            }
        }

        /// <summary>
        /// Vytvoří novou instanci MutableLookup s daným komparátorem klíčů.
        /// </summary>
        /// <param name="comparer">Komparátor pro porovnávání klíčů</param>
        public GroupTable(ILookup<TKey,TValue> values, IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _dictionary = new Dictionary<TKey, List<TValue>>(_comparer);
            
            foreach (var item in values)
            {
                _dictionary.Add(item.Key, item.ToList());
            }
        }

        /// <summary>
        /// Vytvoří novou instanci MutableLookup s daným komparátorem klíčů.
        /// </summary>
        /// <param name="comparer">Komparátor pro porovnávání klíčů</param>
        public GroupTable(IEnumerable<TValue> values, Func<TValue, TKey> keySelector, IEqualityComparer<TKey>? comparer=null)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _dictionary = new Dictionary<TKey, List<TValue>>(_comparer);

            foreach (var item in values)
            {
                var key = keySelector(item);
                if(_dictionary.ContainsKey(key))
                {
                    _dictionary[key].Add(item);
                }
                else
                {
                    _dictionary.Add(key, new List<TValue> { item });
                }
            }
        }

        /// <summary>
        /// Získá kolekci hodnot spojených s daným klíčem.
        /// </summary>
        /// <param name="key">Klíč k vyhledání</param>
        /// <returns>Kolekce hodnot pro daný klíč, nebo prázdná kolekce, pokud klíč neexistuje</returns>
        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                if (_dictionary.TryGetValue(key, out var values))
                {
                    return values.AsReadOnly();
                }

                return [];
            }
        }

        /// <summary>
        /// Zjistí, zda lookup struktura obsahuje daný klíč.
        /// </summary>
        /// <param name="key">Klíč k vyhledání</param>
        /// <returns>True, pokud klíč existuje; jinak False</returns>
        public bool Contains(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Přidá hodnotu pro daný klíč.
        /// </summary>
        /// <param name="key">Klíč</param>
        /// <param name="value">Hodnota k přidání</param>
        public void Add(TKey key, TValue value)
        {
            if (!_dictionary.TryGetValue(key, out var values))
            {
                _dictionary[key] = new List<TValue>();
            }

            values.Add(value);
        }

        /// <summary>
        /// Přidá kolekci hodnot pro daný klíč.
        /// </summary>
        /// <param name="key">Klíč</param>
        /// <param name="values">Kolekce hodnot k přidání</param>
        public void AddRange(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (!_dictionary.TryGetValue(key, out var existingValues))
            {
                _dictionary[key] = new List<TValue>();
            }

            _dictionary[key].AddRange(values);
        }

        /// <summary>
        /// Odstraní všechny výskyty dané hodnoty pro daný klíč.
        /// </summary>
        /// <param name="key">Klíč</param>
        /// <param name="value">Hodnota k odstranění</param>
        /// <returns>Počet odstraněných hodnot</returns>
        public int Remove(TKey key, TValue value)
        {
            if (!_dictionary.TryGetValue(key, out var values))
                return 0;

            int initialCount = values.Count;
            values.RemoveAll(v => EqualityComparer<TValue>.Default.Equals(v, value));

            // Pokud jsme odstranili všechny hodnoty, odstraníme klíč ze slovníku
            if (values.Count == 0)
            {
                _dictionary.Remove(key);
            }

            return initialCount - values.Count;
        }

        /// <summary>
        /// Odstraní všechny hodnoty spojené s daným klíčem.
        /// </summary>
        /// <param name="key">Klíč k odstranění</param>
        /// <returns>True, pokud byl klíč nalezen a odstraněn; jinak False</returns>
        public bool RemoveKey(TKey key)
        {
            return _dictionary.Remove(key);
        }

        /// <summary>
        /// Vyprázdní celou lookup strukturu.
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        /// <summary>
        /// Získá počet hodnot spojených s daným klíčem.
        /// </summary>
        /// <param name="key">Klíč</param>
        /// <returns>Počet hodnot</returns>
        public int CountValues(TKey key)
        {
            if (_dictionary.TryGetValue(key, out var values))
            {
                return values.Count;
            }
            return 0;
        }

        /// <summary>
        /// Vrátí všechny klíče v lookup struktuře.
        /// </summary>
        public IEnumerable<TKey> Keys => _dictionary.Keys;

        /// <summary>
        /// Vrátí všechny hodnoty v lookup struktuře.
        /// </summary>
        public IEnumerable<TValue> Values => _dictionary.Values.SelectMany(list => list);

        /// <summary>
        /// Získá enumerátor pro procházení všech skupin.
        /// </summary>
        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
        {
            foreach (var pair in _dictionary)
            {
                yield return new Grouping<TKey,TValue>(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Získá enumerátor pro procházení všech skupin.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Vytvoří nový MutableLookup ze zdroje dat pomocí selektorů klíče a hodnoty.
        /// </summary>
        /// <typeparam name="TSource">Typ zdrojových dat</typeparam>
        /// <param name="source">Zdrojová data</param>
        /// <param name="keySelector">Funkce pro získání klíče z prvku</param>
        /// <param name="elementSelector">Funkce pro získání hodnoty z prvku</param>
        /// <param name="comparer">Komparátor klíčů (volitelný)</param>
        /// <returns>Nový MutableLookup</returns>
        public static GroupTable<TKey, TValue> FromEnumerable<TSource>(
            IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> elementSelector,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null)
                throw new ArgumentNullException(nameof(elementSelector));

            var lookup = new GroupTable<TKey, TValue>(comparer);

            foreach (var item in source)
            {
                lookup.Add(keySelector(item), elementSelector(item));
            }

            return lookup;
        }
    }
}
