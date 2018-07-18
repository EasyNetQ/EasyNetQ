using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace EasyNetQ
{
    public static class Extensions
    {
        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source, TKey key)
        {
            ((IDictionary<TKey, TValue>) source).Remove(key);
        }

        public static void Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source, TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)source).Add(key, value);        
        }
        
        public static string Trim(this string s, int start, int length)
        {
            // References: https://referencesource.microsoft.com/#mscorlib/system/string.cs,2691
            // https://referencesource.microsoft.com/#mscorlib/system/string.cs,1226
            if (s == null)
            {
                throw new ArgumentNullException();
            }
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            
            var end = start + length - 1;
            if (end >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            for (; start < end; start++)
            {
                if (!char.IsWhiteSpace(s[start]))
                {
                    break;
                }
            }
            for (; end >= start; end--)
            {
                if (!char.IsWhiteSpace(s[end]))
                {
                    break;
                }
            }
            return s.Substring(start, end - start + 1);
        }
    }
}