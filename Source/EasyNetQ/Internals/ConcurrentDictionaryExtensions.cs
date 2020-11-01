using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static void ClearAndDispose<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source)
            where TValue : IDisposable
        {
            source.ClearAndDispose(x => x.Dispose());
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static void ClearAndDispose<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> source, Action<TValue> dispose
        )
        {
            do
            {
                foreach (var key in source.Select(x => x.Key))
                    if (source.TryRemove(key, out var value))
                        dispose(value);
            } while (!source.IsEmpty);
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source, TKey key)
        {
            ((IDictionary<TKey, TValue>)source).Remove(key);
        }
    }
}
