using System.Collections;

namespace Utils.Collections.Collections
{
    public class ReadOnlyList<T> : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        private readonly IList<T> _innerList;

        public ReadOnlyList()
        {
            _innerList = new List<T>();
        }

        public ReadOnlyList(IEnumerable<T> list)
        {
            if (list is not null)
            {
                _innerList = list.ToList(); 
            }
            else
            {
                _innerList = new List<T>();
            }
        }


        public T this[int index] => _innerList[index];

        public int Count => _innerList.Count;

        public IEnumerator<T> GetEnumerator() => _innerList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
