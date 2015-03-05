using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ
{
    internal static class EnumerableExtensions
    {
        private static int index = 0;
        internal static T Random<T>(this IEnumerable<T> enumerable, Func<T, bool> condition) where T : class
        {
            var possibilities = enumerable.Where(condition);
            if (!possibilities.Any()) return null;

            var count = possibilities.Count();
            var response = possibilities.ToArray()[index % count];
            index = (index + 1) % count;
            return response;
        }
    }
}