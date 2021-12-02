using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for <see cref="IAdvancedBus"/>
    /// </summary>
    public static class AdvancedBusExtensions
    {
        /// <summary>
        /// Consume a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="handler">The message handler</param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume<T>(
            this IAdvancedBus bus, Queue queue, Action<IMessage<T>, MessageReceivedInfo> handler
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, handler, _ => { });
        }

        /// <summary>
        /// Consume a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="handler">The message handler</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume<T>(
            this IAdvancedBus bus,
            Queue queue,
            Action<IMessage<T>, MessageReceivedInfo> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            var handlerAsync = TaskHelpers.FromAction<IMessage<T>, MessageReceivedInfo>((m, i, _) => handler(m, i));
            return bus.Consume(queue, handlerAsync, configure);
        }

        /// <summary>
        /// Consume a stream of messages asynchronously
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="handler">The message handler</param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume<T>(
            this IAdvancedBus bus, Queue queue, Func<IMessage<T>, MessageReceivedInfo, Task> handler
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, handler, _ => { });
        }

        /// <summary>
        /// Consume a stream of messages asynchronously
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="handler">The message handler</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume<T>(
            this IAdvancedBus bus,
            Queue queue,
            Func<IMessage<T>, MessageReceivedInfo, Task> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume<T>(queue, (m, i, _) => handler(m, i), configure);
        }

        /// <summary>
        /// Consume a stream of messages asynchronously
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="handler">The message handler</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume<T>(
            this IAdvancedBus bus,
            Queue queue,
            Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume<T>(queue, async (m, i, c) =>
            {
                await handler(m, i, c).ConfigureAwait(false);
                return AckStrategies.Ack;
            }, configure);
        }

        /// <summary>
        /// Consume a stream of messages asynchronously
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="handler">The message handler</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume<T>(
            this IAdvancedBus bus,
            Queue queue,
            IMessageHandler<T> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            var consumeConfiguration = new SimpleConsumeConfiguration();
            configure(consumeConfiguration);

            return bus.Consume(c =>
            {
                if (consumeConfiguration.PrefetchCount.HasValue)
                    c.WithPrefetchCount(consumeConfiguration.PrefetchCount.Value);
                c.ForQueue(
                    queue,
                    handler,
                    p =>
                    {
                        if (consumeConfiguration.ConsumerTag != null)
                            p.WithConsumerTag(consumeConfiguration.ConsumerTag);
                        if (consumeConfiguration.IsExclusive.HasValue)
                            p.WithExclusive(consumeConfiguration.IsExclusive.Value);
                        if (consumeConfiguration.Arguments != null)
                            p.WithArguments(consumeConfiguration.Arguments);
                    }
                );
            });
        }

        /// <summary>
        /// Consume a stream of messages. Dispatch them to the given handlers
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers to the consumer</param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(this IAdvancedBus bus, Queue queue, Action<IHandlerRegistration> addHandlers)
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, addHandlers, _ => { });
        }

        /// <summary>
        /// Consume a stream of messages. Dispatch them to the given handlers
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers to the consumer</param>
        /// <param name="configure">
        ///    Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus,
            Queue queue,
            Action<IHandlerRegistration> addHandlers,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            var consumeConfiguration = new SimpleConsumeConfiguration();
            configure(consumeConfiguration);

            return bus.Consume(c =>
            {
                if (consumeConfiguration.PrefetchCount.HasValue)
                    c.WithPrefetchCount(consumeConfiguration.PrefetchCount.Value);
                c.ForQueue(
                    queue,
                    addHandlers,
                    p =>
                    {
                        if (consumeConfiguration.ConsumerTag != null)
                            p.WithConsumerTag(consumeConfiguration.ConsumerTag);
                        if (consumeConfiguration.IsExclusive.HasValue)
                            p.WithExclusive(consumeConfiguration.IsExclusive.Value);
                        if (consumeConfiguration.Arguments != null)
                            p.WithArguments(consumeConfiguration.Arguments);
                    }
                );
            });
        }

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="handler">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context.
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus, Queue queue, Action<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo> handler
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, handler, _ => { });
        }

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="handler">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus,
            Queue queue,
            Action<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            var handlerAsync = TaskHelpers.FromAction<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo>((m, p, i, _) => handler(m, p, i));

            return bus.Consume(queue, handlerAsync, configure);
        }

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="handler">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context. Returns a Task.
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus,
            Queue queue,
            Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, Task> handler
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, handler, _ => { });
        }

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="handler">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context. Returns a Task.
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus,
            Queue queue,
            Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, Task<AckStrategy>> handler
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, handler, _ => { });
        }

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="handler">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context. Returns a Task.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus,
            Queue queue,
            Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, Task> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, (m, p, i, _) => handler(m, p, i), configure);
        }

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="handler">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context. Returns a Task.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus,
            Queue queue,
            Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, Task<AckStrategy>> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, (m, p, i, _) => handler(m, p, i), configure);
        }

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="handler">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context. Returns a Task.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus,
            Queue queue,
            Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, CancellationToken, Task> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.Consume(queue, async (m, p, i, c) =>
            {
                await handler(m, p, i, c).ConfigureAwait(false);
                return AckStrategies.Ack;
            }, configure);
        }


        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="handler">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context. Returns a Task.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        public static IDisposable Consume(
            this IAdvancedBus bus,
            Queue queue,
            MessageHandler handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            var consumeConfiguration = new SimpleConsumeConfiguration();
            configure(consumeConfiguration);

            return bus.Consume(c =>
            {
                if (consumeConfiguration.PrefetchCount.HasValue)
                    c.WithPrefetchCount(consumeConfiguration.PrefetchCount.Value);
                c.ForQueue(
                    queue,
                    handler,
                    p =>
                    {
                        if (consumeConfiguration.AutoAck)
                            p.WithAutoAck();
                        if (consumeConfiguration.ConsumerTag != null)
                            p.WithConsumerTag(consumeConfiguration.ConsumerTag);
                        if (consumeConfiguration.IsExclusive.HasValue)
                            p.WithExclusive(consumeConfiguration.IsExclusive.Value);
                        if (consumeConfiguration.Arguments != null)
                            p.WithArguments(consumeConfiguration.Arguments);
                    }
                );
            });
        }

        /// <summary>
        /// Declare a queue. If the queue already exists this method does nothing
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The name of the queue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// The queue
        /// </returns>
        public static Task<Queue> QueueDeclareAsync(
            this IAdvancedBus bus,
            string name,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.QueueDeclareAsync(name, _ => { }, cancellationToken);
        }

        /// <summary>
        /// Declare a queue. If the queue already exists this method does nothing
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The name of the queue</param>
        /// <param name="durable">Durable queues remain active when a server restarts.</param>
        /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.</param>
        /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// The queue
        /// </returns>
        public static Task<Queue> QueueDeclareAsync(
            this IAdvancedBus bus,
            string name,
            bool durable,
            bool exclusive,
            bool autoDelete,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.QueueDeclareAsync(
                name,
                c => c.AsDurable(durable)
                    .AsExclusive(exclusive)
                    .AsAutoDelete(autoDelete),
                cancellationToken
            );
        }

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="source">The exchange</param>
        /// <param name="queue">The queue</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        public static Task<Binding<Queue>> BindAsync(
            this IAdvancedBus bus,
            Exchange source,
            Queue queue,
            string routingKey,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.BindAsync(source, queue, routingKey, null, cancellationToken);
        }

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        public static Task<Binding<Exchange>> BindAsync(
            this IAdvancedBus bus,
            Exchange source,
            Exchange destination,
            string routingKey,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.BindAsync(source, destination, routingKey, null, cancellationToken);
        }

        /// <summary>
        /// Declare an exchange
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The exchange name</param>
        /// <param name="type">The type of exchange</param>
        /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
        /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The exchange</returns>
        public static Task<Exchange> ExchangeDeclareAsync(
            this IAdvancedBus bus,
            string name,
            string type,
            bool durable = true,
            bool autoDelete = false,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, nameof(bus));

            return bus.ExchangeDeclareAsync(name, c => c.AsDurable(durable).AsAutoDelete(autoDelete).WithType(type), cancellationToken);
        }

        private class SimpleConsumeConfiguration : ISimpleConsumeConfiguration
        {
            public bool AutoAck { get; private set; }
            public string ConsumerTag { get; private set; }
            public bool? IsExclusive { get; private set; }
            public ushort? PrefetchCount { get; private set; }
            public IDictionary<string, object> Arguments { get; private set; }

            public ISimpleConsumeConfiguration WithAutoAck()
            {
                AutoAck = true;
                return this;
            }

            public ISimpleConsumeConfiguration WithConsumerTag(string consumerTag)
            {
                ConsumerTag = consumerTag;
                return this;
            }

            public ISimpleConsumeConfiguration WithExclusive(bool isExclusive = true)
            {
                IsExclusive = isExclusive;
                return this;
            }

            public ISimpleConsumeConfiguration WithArgument(string name, object value)
            {
                (Arguments ??= new Dictionary<string, object>())[name] = value;
                return this;
            }

            public ISimpleConsumeConfiguration WithPrefetchCount(ushort prefetchCount)
            {
                PrefetchCount = prefetchCount;
                return this;
            }
        }
    }
}
