using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ
{
    public static class LinqExtensions
    {
        // http://mikehadlow.blogspot.co.uk/2012/04/useful-linq-extension-method.html
        public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> items, T separator)
        {
            var first = true;
            foreach (var item in items)
            {
                if (first) first = false;
                else
                {
                    yield return separator;
                }
                yield return item;
            }
        }

        public static IEnumerable<T> SurroundWith<T>(this IEnumerable<T> items, T first, T last)
        {
            yield return first;
            foreach (var item in items)
            {
                yield return item;
            }
            yield return last;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        internal static string Stringify(this IDictionary<string, object> source)
        {
            return string.Join(", ", source.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        [ThreadStatic] private static Random random;

        private static Random Random => random ?? (random = new Random(Environment.TickCount));
    }
}