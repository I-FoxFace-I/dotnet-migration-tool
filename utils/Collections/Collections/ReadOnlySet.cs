using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Utils.Collections.Collections
{
    public class ReadOnlySet<T> : IReadOnlySet<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        private readonly ISet<T> _innerSet;

        public ReadOnlySet()
        {
            _innerSet = new HashSet<T>();
        }

        public ReadOnlySet(IEnumerable<T> collection)
        {
            if(collection is not null)
            {
                _innerSet = collection.ToHashSet();
            }
            else
            {
                _innerSet = new HashSet<T>();
            }
        }

        public ReadOnlySet(ISet<T> set)
        {
            if (set is not null)
            {
                _innerSet = set.ToHashSet();
            }
            else
            {
                _innerSet = new HashSet<T>();
            }
        }

        public int Count => _innerSet.Count;
        public bool Contains(T item) => _innerSet.Contains(item);
        public bool IsProperSubsetOf(IEnumerable<T> other) => _innerSet.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => _innerSet.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => _innerSet.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => _innerSet.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => _innerSet.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => _innerSet.SetEquals(other);
        public IEnumerator<T> GetEnumerator() => _innerSet.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
