using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public sealed class AsyncCache<TKey, TValue> : IDisposable
    {
        private readonly AsyncLock mutex = new AsyncLock();
        private readonly ConcurrentDictionary<TKey, Task<TValue>> storage = new ConcurrentDictionary<TKey, Task<TValue>>();
        private readonly Func<TKey, CancellationToken, Task<TValue>> valueFactory;

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public AsyncCache(Func<TKey, CancellationToken, Task<TValue>> valueFactory)
        {
            this.valueFactory = valueFactory;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key">The key to acquire value from cache</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The existent or created value associated with <paramref name="key"/></returns>
        public Task<TValue> GetOrAddAsync(TKey key, CancellationToken cancellationToken = default)
        {
            return storage.TryGetValue(key, out var existentValue) ? existentValue : GetOrAddInternalAsync(key, cancellationToken);
        }

        private async Task<TValue> GetOrAddInternalAsync(TKey key, CancellationToken cancellationToken)
        {
            using (await mutex.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                if (storage.TryGetValue(key, out var existentValue)) return await existentValue.ConfigureAwait(false);

                var newValue = await valueFactory(key, cancellationToken).ConfigureAwait(false);
                storage[key] = Task.FromResult(newValue);
                return newValue;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            mutex.Dispose();
        }
    }
}
