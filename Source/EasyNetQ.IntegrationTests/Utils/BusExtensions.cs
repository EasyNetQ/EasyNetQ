using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.IntegrationTests.Utils
{
    internal static class BusExtensions
    {
        public static async Task PublishBatchInParallelAsync<T>(
            this IPubSub pubSub, IEnumerable<T> messages, CancellationToken cancellationToken = default
        )
        {
            var publishTasks = new List<Task>();

            foreach (var message in messages)
                publishTasks.Add(pubSub.PublishAsync(message, cancellationToken));

            await Task.WhenAll(publishTasks);
        }

        public static Task PublishBatchAsync<T>(
            this IPubSub pubSub, IEnumerable<T> messages, CancellationToken cancellationToken = default
        )
        {
            return pubSub.PublishBatchAsync(messages, c => { }, cancellationToken);
        }

        public static async Task PublishBatchAsync<T>(
            this IPubSub pubSub,
            IEnumerable<T> messages,
            Action<IPublishConfiguration> configuration,
            CancellationToken cancellationToken = default
        )
        {
            foreach (var message in messages)
                await pubSub.PublishAsync(message, configuration, cancellationToken);
        }

        public static async Task FuturePublishBatchAsync<T>(
            this IScheduler scheduler,
            IEnumerable<T> messages,
            TimeSpan delay,
            string topic = "#",
            CancellationToken cancellationToken = default
        )
        {
            foreach (var message in messages)
                await scheduler.FuturePublishAsync(message, delay, c => c.WithTopic(topic), cancellationToken);
        }

        public static async Task SendBatchAsync<T>(
            this ISendReceive sendReceive,
            string queue,
            IEnumerable<T> messages,
            CancellationToken cancellationToken = default
        )
        {
            foreach (var message in messages)
                await sendReceive.SendAsync(queue, message, cancellationToken);
        }
    }
}
