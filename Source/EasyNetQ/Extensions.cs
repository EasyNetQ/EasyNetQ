using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace EasyNetQ
{
    public static class Extensions
    {
        public static void SafeDispose(this IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch(Exception exception)
            {
                Trace.TraceError(exception.ToString());
            }
        }

        public static TimeSpan Double(this TimeSpan timeSpan)
        {
            return timeSpan + timeSpan;
        }

        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source, TKey key)
        {
            ((IDictionary<TKey, TValue>) source).Remove(key);
        }

        public static void Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source, TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)source).Add(key, value);        
        }
    }

}