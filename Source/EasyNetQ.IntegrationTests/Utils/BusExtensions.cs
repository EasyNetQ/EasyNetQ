using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Scheduling;

namespace EasyNetQ.IntegrationTests.Utils
{
    internal static class BusExtensions
    {
        public static Task PublishBatchAsync<T>(
            this IBus bus, IEnumerable<T> messages, CancellationToken cancellationToken = default
        ) where T : class
        {
            return bus.PublishBatchAsync(messages, c => { }, cancellationToken);
        }

        public static async Task PublishBatchAsync<T>(
            this IBus bus,
            IEnumerable<T> messages,
            Action<IPublishConfiguration> configuration,
            CancellationToken cancellationToken = default
        ) where T : class
        {
            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await bus.PublishAsync(message, configuration).ConfigureAwait(false);
            }
        }

        public static async Task FuturePublishBatchAsync<T>(
            this IBus bus,
            IEnumerable<T> messages,
            TimeSpan delay,
            CancellationToken cancellationToken = default
        ) where T : class
        {
            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await bus.FuturePublishAsync(delay, message).ConfigureAwait(false);
            }
        }

        public static async Task SendBatchAsync<T>(
            this IBus bus,
            string queue,
            IEnumerable<T> messages,
            CancellationToken cancellationToken = default
        ) where T : class
        {
            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await bus.SendAsync(queue, message).ConfigureAwait(false);
            }
        }
    }
}
