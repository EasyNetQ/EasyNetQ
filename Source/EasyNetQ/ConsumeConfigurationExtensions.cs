using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for IPerQueueConsumeConfiguration
    /// </summary>
    public static class PerQueueConsumeConfigurationExtensions
    {
        /// <summary>
        ///     Sets priority
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="priority">The priority to set</param>
        /// <returns>IPerQueueConsumeConfiguration</returns>
        public static IPerQueueConsumeConfiguration WithPriority(this IPerQueueConsumeConfiguration configuration, int priority)
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            configuration.WithArgument("x-priority", priority);
            return configuration;
        }

        /// <summary>
        ///     Adds arguments
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="arguments">The arguments to add</param>
        /// <returns>IPerQueueConsumeConfiguration</returns>
        public static IPerQueueConsumeConfiguration WithArguments(
            this IPerQueueConsumeConfiguration configuration, IDictionary<string, object> arguments
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            Preconditions.CheckNotNull(arguments, nameof(arguments));

            return arguments.Aggregate(configuration, (c, kvp) => c.WithArgument(kvp.Key, kvp.Value));
        }

    }

    /// <summary>
    ///     Various extensions for IConsumeConfiguration
    /// </summary>
    public static class ConsumeConfigurationExtensions
    {
        public static IConsumeConfiguration ForQueue(
            this IConsumeConfiguration configuration,
            Queue queue,
            MessageHandler handler
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            return configuration.ForQueue(queue, handler, c => { });
        }

        public static IConsumeConfiguration ForQueue(
            this IConsumeConfiguration configuration,
            Queue queue,
            Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, CancellationToken, Task> handler
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            return configuration.ForQueue(queue, handler, c => { });
        }

        public static IConsumeConfiguration ForQueue(
            this IConsumeConfiguration configuration,
            Queue queue,
            Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, CancellationToken, Task> handler,
            Action<IPerQueueConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            return configuration.ForQueue(
                queue,
                async (body, properties, receivedInfo, cancellationToken) =>
                {
                    await handler(body, properties, receivedInfo, cancellationToken).ConfigureAwait(false);
                    return AckStrategies.Ack;
                },
                configure
            );
        }

        public static IConsumeConfiguration ForQueue<T>(
            this IConsumeConfiguration configuration,
            Queue queue,
            IMessageHandler<T> handler
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            return configuration.ForQueue(queue, handler, c => { });
        }

        public static IConsumeConfiguration ForQueue<T>(
            this IConsumeConfiguration configuration,
            Queue queue,
            IMessageHandler<T> handler,
            Action<IPerQueueConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            return configuration.ForQueue(queue, x => x.Add(handler), configure);
        }

        public static IConsumeConfiguration ForQueue<T>(
            this IConsumeConfiguration configuration,
            Queue queue,
            Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> handler
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            return configuration.ForQueue(queue, handler, c => { });
        }

        public static IConsumeConfiguration ForQueue<T>(
            this IConsumeConfiguration configuration,
            Queue queue,
            Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> handler,
            Action<IPerQueueConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            return configuration.ForQueue(queue, x => x.Add(handler), configure);
        }

        public static IConsumeConfiguration ForQueue<T>(
            this IConsumeConfiguration configuration,
            Queue queue,
            Action<IMessage<T>, MessageReceivedInfo> handler
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            return configuration.ForQueue(queue, handler, c => { });
        }

        public static IConsumeConfiguration ForQueue<T>(
            this IConsumeConfiguration configuration,
            Queue queue,
            Action<IMessage<T>, MessageReceivedInfo> handler,
            Action<IPerQueueConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            return configuration.ForQueue(queue, x => x.Add(handler), configure);
        }
    }

    /// <summary>
    ///     Various extensions for ISimpleConsumeConfiguration
    /// </summary>
    public static class SimpleConsumeConfigurationExtensions
    {
        /// <summary>
        ///     Sets priority
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="priority">The priority to set</param>
        /// <returns>ISimpleConsumeConfiguration</returns>
        public static ISimpleConsumeConfiguration WithPriority(
            this ISimpleConsumeConfiguration configuration, int priority
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            configuration.WithArgument("x-priority", priority);
            return configuration;
        }
    }
}
