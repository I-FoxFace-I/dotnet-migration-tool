using System.Collections;

namespace Utils.Collections.Collections
{
    /// <summary>
    /// Jednoduchá implementace IOrderedEnumerable<T> pro použití jako výchozí implementace.
    /// </summary>
    /// <typeparam name="T">Typ prvků v kolekci</typeparam>
    public class OrderedEnumerable<T> : IOrderedEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// Vytvoří novou instanci SimpleOrderedEnumerable s výchozím porovnávačem.
        /// </summary>
        public OrderedEnumerable() : this(Enumerable.Empty<T>(), Comparer<T>.Default)
        {
        }

        /// <summary>
        /// Vytvoří novou instanci SimpleOrderedEnumerable s danou zdrojovou kolekcí.
        /// </summary>
        /// <param name="source">Zdrojová kolekce prvků</param>
        public OrderedEnumerable(IEnumerable<T> source) : this(source, Comparer<T>.Default)
        {
        }

        /// <summary>
        /// Vytvoří novou instanci SimpleOrderedEnumerable s danou zdrojovou kolekcí a porovnávačem.
        /// </summary>
        /// <param name="source">Zdrojová kolekce prvků</param>
        /// <param name="comparer">Porovnávač pro řazení prvků</param>
        public OrderedEnumerable(IEnumerable<T> source, IComparer<T> comparer)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// Vytvoří novou instanci typu <see cref="OrderedEnumerable{T}"/>, která reprezentuje
        /// následnou aplikaci sekundárního řazení.
        /// </summary>
        /// <typeparam name="TKey">Typ klíče pro sekundární řazení</typeparam>
        /// <param name="keySelector">Funkce pro výběr klíče sekundárního řazení</param>
        /// <param name="comparer">Porovnávač pro sekundární řazení</param>
        /// <param name="descending">True pro sestupné řazení, jinak false</param>
        /// <returns>Nová instance IOrderedEnumerable se sekundárním řazením</returns>
        public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(
            Func<T, TKey> keySelector,
            IComparer<TKey> comparer,
            bool descending)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            // Předáme aktuální seřazené prvky jako zdroj pro nové řazení
            IEnumerable<T> sortedSource = this.ToList(); // Musíme materializovat, abychom zachovali původní řazení

            // Aplikujeme nové řazení
            IEnumerable<T> newSortedSource;
            if (descending)
            {
                newSortedSource = sortedSource.OrderByDescending(keySelector, comparer ?? Comparer<TKey>.Default);
            }
            else
            {
                newSortedSource = sortedSource.OrderBy(keySelector, comparer ?? Comparer<TKey>.Default);
            }

            // Vrátíme novou instanci s novým řazením
            return new OrderedEnumerable<T>(newSortedSource, _comparer);
        }

        /// <summary>
        /// Vrací enumerátor, který iteruje kolekci.
        /// </summary>
        /// <returns>Enumerátor kolekce</returns>
        public IEnumerator<T> GetEnumerator()
        {
            // Vrátíme enumerátor seřazené kolekce
            return _source.OrderBy(x => x, _comparer).GetEnumerator();
        }

        /// <summary>
        /// Vrací ne-generický enumerátor, který iteruje kolekci.
        /// </summary>
        /// <returns>Ne-generický enumerátor kolekce</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
