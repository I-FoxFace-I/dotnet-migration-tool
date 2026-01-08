using System.Collections;

namespace Utils.Collections.Collections
{
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly TKey _key;
        private readonly IEnumerable<TElement> _values;

        public Grouping()
        {
            _key = (TKey)new object();
            _values = [];
        }

        public Grouping(TKey key, IEnumerable<TElement> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            _key = key;
            _values = values;
        }

        public Grouping(IGrouping<TKey, TElement> grouping)
        {
            if (grouping == null)
                throw new ArgumentNullException("grouping");
            _key = grouping.Key;
            _values = grouping.ToArray();
        }

        public TKey Key
        {
            get { return _key; }
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
