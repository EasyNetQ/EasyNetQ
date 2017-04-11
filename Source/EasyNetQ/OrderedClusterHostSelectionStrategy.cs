using System.Collections;
using System.Collections.Generic;

namespace EasyNetQ
{
    /// <summary>
    /// A collection that hands out the next item until success, or until every item has been tried.
    /// </summary>
    public class OrderedClusterHostSelectionStrategy<T> : IClusterHostSelectionStrategy<T>, IEnumerable<T> where T : class
    {
        private readonly IList<T> items = new List<T>();
        private int currentIndex;
        private int startIndex;

        public virtual void Add(T item)
        {
            Preconditions.CheckNotNull(item, "item");
            items.Add(item);
            startIndex = items.Count-1;
        }

        public virtual T Current()
        {
            if (items.Count == 0)
            {
                throw new EasyNetQException("No items in collection");
            }

            return items[currentIndex];
        }

        public virtual bool Next()
        {
            if (currentIndex == startIndex) return false;
            if (Succeeded) return false;

            IncrementIndex();

            return true;
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void Success()
        {
            Succeeded = true;
            startIndex = currentIndex;
        }

        public virtual bool Succeeded { get; private set; }

        private bool firstUse = true;

        public OrderedClusterHostSelectionStrategy()
        {
            Succeeded = false;
        }

        public virtual void Reset()
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