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
            Preconditions.CheckNotNull(enumerable,"enumerable", "Null collection passed to Random()");
            Preconditions.CheckAny(enumerable, "enumerable", "Empty collection passed to Random()");

            var possibilities = enumerable.Where(condition);
            if (!possibilities.Any()) return enumerable.First();

            var count = possibilities.Count();
            var response = possibilities.ToArray()[index % count];
            index = (index + 1) % count;
            return response;
        }
    }
}