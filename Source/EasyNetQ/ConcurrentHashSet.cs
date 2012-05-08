using System.Collections;
using System.Collections.Generic;

namespace EasyNetQ
{
    /// <summary>
    /// Why isn't this in the BCL??
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentHashSet<T> : ISet<T>
    {
        readonly ISet<T> internalSet = new HashSet<T>(); 
        readonly object padlock = new object();

        public IEnumerator<T> GetEnumerator()
        {
            return internalSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<T>.Add(T item)
        {
            internalSet.Add(item);
        }

        public bool Add(T item)
        {
            lock (padlock)
            {
                return internalSet.Add(item);
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            internalSet.UnionWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            internalSet.IntersectWith(other);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            internalSet.ExceptWith(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            internalSet.SymmetricExceptWith(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return internalSet.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return internalSet.IsSupersetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return internalSet.IsProperSupersetOf(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return internalSet.IsProperSubsetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return internalSet.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return internalSet.SetEquals(other);
        }

        public void Clear()
        {
            internalSet.Clear();
        }

        public bool Contains(T item)
        {
            lock (padlock)
            {
                return internalSet.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            internalSet.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return internalSet.Remove(item);
        }

        public int Count
        {
            get { return internalSet.Count; }
        }

        public bool IsReadOnly
        {
            get { return internalSet.IsReadOnly; }
        }
    }
}