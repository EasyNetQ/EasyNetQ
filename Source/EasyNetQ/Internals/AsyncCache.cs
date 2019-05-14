using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public sealed class AsyncCache<TKey, TValue>
    {
        private readonly AsyncLock mutex = new AsyncLock();
        private readonly ConcurrentDictionary<TKey, Task<TValue>> storage = new ConcurrentDictionary<TKey, Task<TValue>>();
        private readonly Func<TKey, CancellationToken, Task<TValue>> valueFactory;

        public AsyncCache(Func<TKey, CancellationToken, Task<TValue>> valueFactory)
        {
            this.valueFactory = valueFactory;
        }

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
    }
}
