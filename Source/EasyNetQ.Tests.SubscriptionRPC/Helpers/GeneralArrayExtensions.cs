using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyNetQ.Tests.SubscriptionRPC.Helpers {
    public static class GeneralArrayExtensions {
        /// <summary>
        /// If the Array is Null it will return an Empty Array instead
        /// </summary>
        public static IEnumerable<T> Safe<T>(this IEnumerable<T> items, T[] empty = null) {
            return items != null ? items : empty ?? new T[0];
        }

        public static bool NotNullOrEmpty<T>(this IEnumerable<T> items) {
            return items != null && items.Any();
        }

        public static bool IsNull<T>(this IEnumerable<T> items) {
            return items == null;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> items) {
            return items == null || !items.Any();
        }

        public static IEnumerable<T> TakeColumn<T>(this IEnumerable<T> items, int page, int size) {
            if (items.IsNullOrEmpty()) return items;
            var count = items.Count();
            return items.Paging(page, count / size);
        }

        public static void Each<T>(this IEnumerable<T> items, Action<T, int> action) {
            if (items.IsNullOrEmpty()) return;
            int i = 0;
            foreach (var item in items)
                action(item, i++);
        }

        public static void Each<T>(this IEnumerable<T> items, Action<T> action) {
            if (items.IsNullOrEmpty()) return;
            foreach (var item in items)
                action(item);
        }

        public static IQueryable<T> Paging<T>(this IQueryable<T> query, int page, int size) {
            return query.Skip(page * size).Take(size);
        }

        public static IEnumerable<T> Paging<T>(this IEnumerable<T> query, int page, int size) {
            return query.Skip(page * size).Take(size);
        }

        public static IEnumerable<T> AddLast<T>(this IEnumerable<T> collection, int n) {
            if (collection == null || n < 0) return null;
            LinkedList<T> temp = new LinkedList<T>();

            foreach (var value in collection) {
                temp.AddLast(value);
                if (temp.Count > n)
                    temp.RemoveFirst();
            }

            return temp;
        }

        public static void AddRange<T>(this ICollection<T> items, IEnumerable<T> values) {
            items.AddRange(values, v => v != null);
        }

        /// <summary>
        /// Adds a Range of String.  Will remove any strings that are Null, Empty or Whitespace
        /// </summary>
        public static void AddRange(this ICollection<string> items, IEnumerable<string> values) {
            items.AddRange(values, v => !string.IsNullOrWhiteSpace(v));
        }

        public static void AddRange<T>(this ICollection<T> items, IEnumerable<T> values, Func<T, bool> where) {
            if (items.IsNull() || values.IsNullOrEmpty()) return;
            foreach (var item in values.Where(where))
                items.Add(item);
        }

        public static bool ContainsAny<T>(this IEnumerable<T> first, T second) {
            if (first.IsNullOrEmpty())
                return false;
            return first.Any(v => v.Equals(second));
        }

        public static Dictionary<string, int> Merge(this IEnumerable<Dictionary<string, int>> results) {
            Dictionary<string, int> result = null;
            try {
                if (results.IsNullOrEmpty())
                    return new Dictionary<string, int>();

                bool first = true;
                foreach (var dictionary in results)
                    if (first) {
                        result = new Dictionary<string, int>(dictionary);
                        first = false;
                    }
                    else foreach (var item in dictionary)
                            if (!result.ContainsKey(item.Key))
                                result.Add(item.Key, item.Value);
                            else result[item.Key] += item.Value;

                return result;
            }
            finally {
                results = null;
                result = null;
            }
        }

        public static double WeightedAverage<T>(this IEnumerable<T> records, Func<T, double> value, Func<T, double> weight) {
            double weightedValueSum = records.Sum(record => value(record) * weight(record));
            double weightSum = records.Sum(record => weight(record));

            if (weightSum != 0)
                return weightedValueSum / weightSum;
            else
                return 0;
        }

        public static double WeightedAverage<T>(this IEnumerable<T> records, Func<T, double> value, Func<T, int> weight) {
            double weightedValueSum = records.Sum(record => value(record) * weight(record));
            double weightSum = records.Sum(record => weight(record));

            if (weightSum != 0)
                return weightedValueSum / weightSum;
            else
                return 0;
        }
    }
}
