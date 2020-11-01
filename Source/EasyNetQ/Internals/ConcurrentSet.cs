using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public sealed class ConcurrentSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, bool> dictionary = new ConcurrentDictionary<T, bool>();

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.Select(x => x.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an element to the set
        /// </summary>
        /// <param name="element">The element to add</param>
        public void Add(T element)
        {
            dictionary.TryAdd(element, default);
        }

        /// <summary>
        /// Removes an element to the set
        /// </summary>
        /// <param name="element">The element to remove</param>
        public void Remove(T element)
        {
            dictionary.TryRemove(element, out _);
        }

        /// <summary>
        /// Clears the set
        /// </summary>
        public void Clear()
        {
            dictionary.Clear();
        }
    }
}
