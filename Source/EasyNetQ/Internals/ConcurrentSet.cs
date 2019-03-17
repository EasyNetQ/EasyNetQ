using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Internals
{
    public sealed class ConcurrentSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, bool> dictionary = new ConcurrentDictionary<T, bool>();

        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.Select(x => x.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Add(T element)
        {
            return dictionary.TryAdd(element, default);
        }

        public bool Remove(T element)
        {
            return dictionary.TryRemove(element, out _);
        }

        public void Clear()
        {
            dictionary.Clear();
        }
    }
}
