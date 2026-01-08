using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IvoEngine.Collections.DataStructures;

/// <summary>
/// Vysoce optimalizovaná implementace hashovací tabulky pro maximální výkon.
/// </summary>
/// <typeparam name="TKey">Typ klíče</typeparam>
/// <typeparam name="TValue">Typ hodnoty</typeparam>
public class OptimizedHashTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    // Konstanty pro výkon
    private const int DEFAULT_CAPACITY = 16;
    private const float LOAD_FACTOR = 0.75f;
    private const int STACK_ALLOC_THRESHOLD = 128; // Hranice pro alokaci na zásobníku

    // Optimalizovaná struktura Entry - použití struct místo class pro lepší lokalitu cache
    private struct Entry
    {
        public int HashCode;      // Uložený hash kód pro rychlejší porovnání
        public int Next;          // Index dalšího prvku v bucket řetězci (-1 = konec)
        public TKey Key;          // Klíč
        public TValue Value;      // Hodnota
    }

    // Pole bucketů - indexy do pole entries
    private int[] _buckets;

    // Pole všech položek
    private Entry[] _entries;

    // Počet prvků v tabulce
    private int _count;

    // Volný index v poli entries
    private int _freeList;

    // Počet volných míst v _entries
    private int _freeCount;

    // Počet modifikací pro ochranu enumerátoru
    private int _version;

    // Vlastní komparátor klíčů
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>
    /// Vytvoří novou optimalizovanou hashovací tabulku.
    /// </summary>
    public OptimizedHashTable() : this(DEFAULT_CAPACITY, null)
    {
    }

    /// <summary>
    /// Vytvoří novou optimalizovanou hashovací tabulku s daným komparátorem.
    /// </summary>
    public OptimizedHashTable(IEqualityComparer<TKey> comparer)
        : this(DEFAULT_CAPACITY, comparer)
    {
    }

    /// <summary>
    /// Vytvoří novou optimalizovanou hashovací tabulku s danou kapacitou.
    /// </summary>
    public OptimizedHashTable(int capacity) : this(capacity, null)
    {
    }

    /// <summary>
    /// Vytvoří novou optimalizovanou hashovací tabulku s danou kapacitou a komparátorem.
    /// </summary>
    public OptimizedHashTable(int capacity, IEqualityComparer<TKey> comparer)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        // Optimalizace - získání nejbližší mocniny 2 pro kapacitu
        int size = capacity > 0 ? GetNextPowerOfTwo(capacity) : DEFAULT_CAPACITY;

        _buckets = new int[size];
        _entries = new Entry[size];

        // Inicializace na -1 (žádný prvek)
        Array.Fill(_buckets, -1);

        _freeList = -1;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
    }

    /// <summary>
    /// Počet prvků v hashovací tabulce.
    /// </summary>
    public int Count => _count - _freeCount;

    /// <summary>
    /// Vrátí hodnotu pro daný klíč nebo nastaví novou hodnotu.
    /// </summary>
    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int i = FindEntry(key);
            if (i >= 0)
                return _entries[i].Value;
            throw new KeyNotFoundException();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Insert(key, value, false);
    }

    /// <summary>
    /// Přidá nebo aktualizuje hodnotu pro daný klíč.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value)
    {
        Insert(key, value, true);
    }

    /// <summary>
    /// Pokusí se přidat nový pár klíč-hodnota.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(TKey key, TValue value)
    {
        try
        {
            Insert(key, value, true);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Pokusí se získat hodnotu pro daný klíč.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        int i = FindEntry(key);
        if (i >= 0)
        {
            value = _entries[i].Value;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Zjistí, zda tabulka obsahuje daný klíč.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        return FindEntry(key) >= 0;
    }

    /// <summary>
    /// Pokusí se odstranit záznam s daným klíčem.
    /// </summary>
    public bool Remove(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (_buckets == null || _count == 0)
            return false;

        int hashCode = GetHashCode(key);
        int bucket = hashCode & _buckets.Length - 1;
        int last = -1;

        // Optimalizace - přímý průchod číselným indexovým řetězcem místo referencí
        for (int i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].Next)
        {
            // Optimalizace - nejprve porovnat hash kódy, až pak Equals
            if (_entries[i].HashCode == hashCode && _comparer.Equals(_entries[i].Key, key))
            {
                if (last < 0)
                {
                    _buckets[bucket] = _entries[i].Next;
                }
                else
                {
                    _entries[last].Next = _entries[i].Next;
                }

                // Označit jako smazaný (využít volný list pro recyklaci)
                _entries[i].HashCode = -1;
                _entries[i].Next = _freeList;
                _entries[i].Key = default;
                _entries[i].Value = default;
                _freeList = i;
                _freeCount++;
                _version++;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Vyprázdní hashovací tabulku.
    /// </summary>
    public void Clear()
    {
        if (_count > 0)
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            Array.Fill(_buckets, -1);
            Array.Clear(_entries, 0, _count);
            _freeList = -1;
            _count = 0;
            _freeCount = 0;
            _version++;
        }
    }

    /// <summary>
    /// Najde index záznamu pro daný klíč.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindEntry(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (_buckets != null && _count > 0)
        {
            int hashCode = GetHashCode(key);

            // Optimalizace - bitová operace místo modulo
            int bucket = hashCode & _buckets.Length - 1;

            // Optimalizace - přímý průchod indexovým řetězcem
            for (int i = _buckets[bucket]; i >= 0; i = _entries[i].Next)
            {
                // Nejprve porovnat hash kódy, pak až Equals pro rychlejší filtrování
                if (_entries[i].HashCode == hashCode && _comparer.Equals(_entries[i].Key, key))
                    return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Vloží nový prvek nebo aktualizuje existující.
    /// </summary>
    private void Insert(TKey key, TValue value, bool add)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        // Pokud nemáme buckets nebo je potřeba zvětšit
        if (_buckets == null || _count >= _entries.Length * LOAD_FACTOR)
        {
            Resize();
        }

        int hashCode = GetHashCode(key);

        // Optimalizace - použití bitové masky místo modulo
        int bucket = hashCode & _buckets.Length - 1;

        // Nejprve zkontrolujeme, zda klíč existuje
        for (int i = _buckets[bucket]; i >= 0; i = _entries[i].Next)
        {
            if (_entries[i].HashCode == hashCode && _comparer.Equals(_entries[i].Key, key))
            {
                if (add)
                    throw new ArgumentException("Klíč již existuje.", nameof(key));

                _entries[i].Value = value;
                _version++;
                return;
            }
        }

        int index;
        if (_freeCount > 0)
        {
            // Využijeme recyklovanou položku
            index = _freeList;
            _freeList = _entries[index].Next;
            _freeCount--;
        }
        else
        {
            // Přidáme novou položku na konec
            if (_count == _entries.Length)
            {
                Resize();
                // Přepočítáme bucket po změně velikosti
                bucket = hashCode & _buckets.Length - 1;
            }
            index = _count;
            _count++;
        }

        // Vložíme na začátek řetězce v bucketu
        _entries[index].HashCode = hashCode;
        _entries[index].Next = _buckets[bucket];
        _entries[index].Key = key;
        _entries[index].Value = value;
        _buckets[bucket] = index;
        _version++;
    }

    /// <summary>
    /// Změní velikost hashovací tabulky.
    /// </summary>
    private void Resize()
    {
        // Dvojnásobná velikost nebo výchozí
        int newSize = _entries != null ? _entries.Length * 2 : DEFAULT_CAPACITY;

        // Nové bucket pole
        int[] newBuckets = new int[newSize];
        Array.Fill(newBuckets, -1);

        // Nové entries pole
        Entry[] newEntries = new Entry[newSize];

        // Kopírování existujících položek
        if (_entries != null)
        {
            Array.Copy(_entries, 0, newEntries, 0, _count);
        }

        // Přebudování všech řetězců
        for (int i = 0; i < _count; i++)
        {
            // Přeskočit smazané položky
            if (newEntries[i].HashCode >= 0)
            {
                int bucket = newEntries[i].HashCode & newSize - 1;
                newEntries[i].Next = newBuckets[bucket];
                newBuckets[bucket] = i;
            }
        }

        _buckets = newBuckets;
        _entries = newEntries;
    }

    /// <summary>
    /// Optimalizovaná hash funkce.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetHashCode(TKey key)
    {
        int hash = _comparer.GetHashCode(key) & 0x7FFFFFFF;

        // Sekundární hašování pro lepší distribuci
        hash = (hash << 5) + hash ^ hash >> 27;
        return hash & 0x7FFFFFFF;
    }

    /// <summary>
    /// Získá další mocninu 2 větší nebo rovnou danému číslu.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNextPowerOfTwo(int value)
    {
        int result = 1;
        while (result < value)
        {
            result <<= 1;
        }
        return result;
    }

    /// <summary>
    /// Získá enumerátor pro procházení všech párů klíč-hodnota.
    /// </summary>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// Získá enumerátor pro procházení všech párů klíč-hodnota.
    /// </summary>
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// Získá enumerátor pro procházení všech párů klíč-hodnota.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// Optimalizovaný enumerátor s minimální režií.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator
    {
        private readonly OptimizedHashTable<TKey, TValue> _dictionary;
        private readonly int _version;
        private int _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(OptimizedHashTable<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _version = dictionary._version;
            _index = 0;
            _current = default;
        }

        public KeyValuePair<TKey, TValue> Current => _current;

        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Kolekce byla změněna během enumerace.");

            // Přeskočit smazané položky a najít další platnou položku
            while (_index < _dictionary._count)
            {
                if (_dictionary._entries[_index].HashCode >= 0)
                {
                    _current = new KeyValuePair<TKey, TValue>(
                        _dictionary._entries[_index].Key,
                        _dictionary._entries[_index].Value);
                    _index++;
                    return true;
                }
                _index++;
            }

            _current = default;
            return false;
        }

        public void Reset()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException("Kolekce byla změněna během enumerace.");

            _index = 0;
            _current = default;
        }

        public void Dispose()
        {
            // Není co uvolňovat, je to struktura
        }
    }

    /// <summary>
    /// Získá kolekci všech klíčů.
    /// </summary>
    public KeyCollection Keys => new KeyCollection(this);

    /// <summary>
    /// Optimalizovaná kolekce klíčů.
    /// </summary>
    public struct KeyCollection : IEnumerable<TKey>
    {
        private readonly OptimizedHashTable<TKey, TValue> _dictionary;

        public KeyCollection(OptimizedHashTable<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_dictionary);
        }

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
        {
            return new Enumerator(_dictionary);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_dictionary);
        }

        public struct Enumerator : IEnumerator<TKey>, IEnumerator
        {
            private OptimizedHashTable<TKey, TValue>.Enumerator _dictEnum;

            internal Enumerator(OptimizedHashTable<TKey, TValue> dictionary)
            {
                _dictEnum = dictionary.GetEnumerator();
            }

            public TKey Current => _dictEnum.Current.Key;

            object IEnumerator.Current => Current;

            public bool MoveNext() => _dictEnum.MoveNext();

            public void Reset() => _dictEnum.Reset();

            public void Dispose() => _dictEnum.Dispose();
        }
    }

    /// <summary>
    /// Získá kolekci všech hodnot.
    /// </summary>
    public ValueCollection Values => new ValueCollection(this);

    /// <summary>
    /// Optimalizovaná kolekce hodnot.
    /// </summary>
    public struct ValueCollection : IEnumerable<TValue>
    {
        private readonly OptimizedHashTable<TKey, TValue> _dictionary;

        public ValueCollection(OptimizedHashTable<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_dictionary);
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return new Enumerator(_dictionary);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_dictionary);
        }

        public struct Enumerator : IEnumerator<TValue>, IEnumerator
        {
            private OptimizedHashTable<TKey, TValue>.Enumerator _dictEnum;

            internal Enumerator(OptimizedHashTable<TKey, TValue> dictionary)
            {
                _dictEnum = dictionary.GetEnumerator();
            }

            public TValue Current => _dictEnum.Current.Value;

            object IEnumerator.Current => Current;

            public bool MoveNext() => _dictEnum.MoveNext();

            public void Reset() => _dictEnum.Reset();

            public void Dispose() => _dictEnum.Dispose();
        }
    }
}
