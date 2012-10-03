using System.Collections;
using System.Collections.Generic;

namespace EasyNetQ
{
    /// <summary>
    /// A collection that hands out the next item until success, or until every item has been tried.
    /// </summary>
    public class DefaultClusterHostSelectionStrategy<T> : IClusterHostSelectionStrategy<T>, IEnumerable<T>
    {
        private readonly IList<T> items = new List<T>();
        private int currentIndex = 0;
        private int startIndex = 0;

        public void Add(T item)
        {
            items.Add(item);
            startIndex = items.Count-1;
        }

        public T Current()
        {
            if (items.Count == 0)
            {
                throw new EasyNetQException("No items in collection");
            }

            return items[currentIndex];
        }

        public bool Next()
        {
            if (currentIndex == startIndex) return false;
            if (Succeeded) return false;

            IncrementIndex();

            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Success()
        {
            Succeeded = true;
            startIndex = currentIndex;
        }

        public bool Succeeded { get; private set; }

        private bool firstUse = true;

        public DefaultClusterHostSelectionStrategy()
        {
            Succeeded = false;
        }

        public void Reset()
        {
            Succeeded = false;
            if (firstUse)
            {
                firstUse = false;
                return;
            }
            IncrementIndex();
        }

        private void IncrementIndex()
        {
            currentIndex++;
            if (currentIndex == items.Count)
            {
                currentIndex = 0;
            }
        }
    }
}