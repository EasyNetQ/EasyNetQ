using System.Collections.Generic;

namespace EasyNetQ
{
    public static class Extensions
    {
        public static IEnumerable<T> ToEnumerable<T>(this T item)
        {
            yield return item;
        }
    }
}