﻿using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for IAdvancedBus
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
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume(queue, handler, c => { });
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
            Preconditions.CheckNotNull(bus, "bus");

            var handlerAsync = TaskHelpers.FromAction<IMessage<T>, MessageReceivedInfo>((m, i, c) => handler(m, i));
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
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume(queue, handler, c => { });
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
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume<T>(queue, (m, i, c) => handler(m, i), configure);
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
            Preconditions.CheckNotNull(bus, "bus");

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
            Preconditions.CheckNotNull(bus, "bus");

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
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume(queue, addHandlers, c => { });
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
            Preconditions.CheckNotNull(bus, "bus");

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
            this IAdvancedBus bus, Queue queue, Action<byte[], MessageProperties, MessageReceivedInfo> handler
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume(queue, handler, c => { });
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
            Action<byte[], MessageProperties, MessageReceivedInfo> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            var handlerAsync = TaskHelpers.FromAction<byte[], MessageProperties, MessageReceivedInfo>((m, p, i, c) => handler(m, p, i));

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
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> handler
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume(queue, handler, c => { });
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
            Func<byte[], MessageProperties, MessageReceivedInfo, Task<AckStrategy>> handler
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume(queue, handler, c => { });
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
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume(queue, (m, p, i, c) => handler(m, p, i), configure);
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
            Func<byte[], MessageProperties, MessageReceivedInfo, Task<AckStrategy>> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.Consume(queue, (m, p, i, c) => handler(m, p, i), configure);
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
            Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> handler,
            Action<ISimpleConsumeConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

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
            Preconditions.CheckNotNull(bus, "bus");

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
        ///     Publish a message as a byte array
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">
        ///     The routing key for the message. The routing key is used for routing messages depending on the
        ///     exchange configuration.
        /// </param>
        /// <param name="mandatory">
        ///     This flag tells the server how to react if the message cannot be routed to a queue.
        ///     If this flag is true, the server will return an unroutable message with a Return method.
        ///     If this flag is false, the server silently drops the message.
        /// </param>
        /// <param name="messageProperties">The message properties</param>
        /// <param name="body">The message body</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish(
            this IAdvancedBus bus,
            Exchange exchange,
            string routingKey,
            bool mandatory,
            MessageProperties messageProperties,
            byte[] body,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(exchange, routingKey, mandatory, messageProperties, body, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        ///     Publish a message as a .NET type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">
        ///     The routing key for the message. The routing key is used for routing messages depending on the
        ///     exchange configuration.
        /// </param>
        /// <param name="mandatory">
        ///     This flag tells the server how to react if the message cannot be routed to a queue.
        ///     If this flag is true, the server will return an unroutable message with a Return method.
        ///     If this flag is false, the server silently drops the message.
        /// </param>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish<T>(
            this IAdvancedBus bus,
            Exchange exchange,
            string routingKey,
            bool mandatory,
            IMessage<T> message,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(exchange, routingKey, mandatory, message, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Gets stats for the given queue
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The stats of the queue</returns>
        public static QueueStats GetQueueStats(
            this IAdvancedBus bus, Queue queue, CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.GetQueueStatsAsync(queue, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Declare a transient server named queue. Note, this queue will only last for duration of the
        /// connection. If there is a connection outage, EasyNetQ will not attempt to recreate
        /// consumers.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The queue</returns>
        public static Queue QueueDeclare(this IAdvancedBus bus, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.QueueDeclareAsync(cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Declare a queue. If the queue already exists this method does nothing
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The name of the queue</param>
        /// <param name="configure">Delegate to configure the queue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// The queue
        /// </returns>
        public static Queue QueueDeclare(
            this IAdvancedBus bus,
            string name,
            Action<IQueueDeclareConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.QueueDeclareAsync(name, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
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
            Preconditions.CheckNotNull(bus, "bus");

            return bus.QueueDeclareAsync(name, c => { }, cancellationToken);
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
            Preconditions.CheckNotNull(bus, "bus");

            return bus.QueueDeclareAsync(
                name,
                c => c.AsDurable(durable)
                    .AsExclusive(exclusive)
                    .AsAutoDelete(autoDelete),
                cancellationToken
            );
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
        public static Queue QueueDeclare(
            this IAdvancedBus bus,
            string name,
            bool durable,
            bool exclusive,
            bool autoDelete,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.QueueDeclareAsync(name, durable, exclusive, autoDelete, cancellationToken)
                .GetAwaiter()
                .GetResult();
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
        public static Queue QueueDeclare(
            this IAdvancedBus bus,
            string name,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.QueueDeclareAsync(name, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Declare a queue passively. Throw an exception rather than create the queue if it doesn't exist
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The queue to declare</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void QueueDeclarePassive(
            this IAdvancedBus bus,
            string name,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.QueueDeclarePassiveAsync(name, cancellationToken)
                .GetAwaiter()
                .GetResult();
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
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

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
            Preconditions.CheckNotNull(bus, "bus");

            return bus.BindAsync(source, destination, routingKey, null, cancellationToken);
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
        public static Binding<Exchange> Bind(this IAdvancedBus bus, Exchange source, Exchange destination, string routingKey, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.BindAsync(source, destination, routingKey, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        public static Binding<Exchange> Bind(
            this IAdvancedBus bus,
            Exchange source,
            Exchange destination,
            string routingKey,
            IDictionary<string, object> headers,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.BindAsync(source, destination, routingKey, headers, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        public static Binding<Queue> Bind(this IAdvancedBus bus, Exchange exchange, Queue queue, string routingKey, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.BindAsync(exchange, queue, routingKey, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        public static Binding<Queue> Bind(
            this IAdvancedBus bus,
            Exchange exchange,
            Queue queue,
            string routingKey,
            IDictionary<string, object> headers,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.BindAsync(exchange, queue, routingKey, headers, cancellationToken)
                .GetAwaiter()
                .GetResult();
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
            Preconditions.CheckNotNull(bus, "bus");

            return bus.ExchangeDeclareAsync(name, c => c.AsDurable(durable).AsAutoDelete(autoDelete).WithType(type), cancellationToken);
        }

        /// <summary>
        /// Declare a exchange passively. Throw an exception rather than create the exchange if it doesn't exist
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The exchange to declare</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void ExchangeDeclarePassive(
            this IAdvancedBus bus,
            string name,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.ExchangeDeclarePassiveAsync(name, cancellationToken)
                .GetAwaiter()
                .GetResult();
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
        public static Exchange ExchangeDeclare(
            this IAdvancedBus bus,
            string name,
            string type,
            bool durable = true,
            bool autoDelete = false,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.ExchangeDeclareAsync(name, type, durable, autoDelete, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Declare an exchange
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The exchange name</param>
        /// <param name="configure">The configuration of exchange</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The exchange</returns>
        public static Exchange ExchangeDeclare(
            this IAdvancedBus bus,
            string name,
            Action<IExchangeDeclareConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.ExchangeDeclareAsync(name, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Delete a binding
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="binding">the binding to delete</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Unbind(this IAdvancedBus bus, Binding<Queue> binding, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.UnbindAsync(binding, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Delete a binding
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="binding">the binding to delete</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Unbind(this IAdvancedBus bus, Binding<Exchange> binding, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.UnbindAsync(binding, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Delete a queue
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to delete</param>
        /// <param name="ifUnused">Only delete if unused</param>
        /// <param name="ifEmpty">Only delete if empty</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void QueueDelete(this IAdvancedBus bus, Queue queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.QueueDeleteAsync(queue, ifUnused, ifEmpty, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Purges a queue
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to purge</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void QueuePurge(this IAdvancedBus bus, Queue queue, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.QueuePurgeAsync(queue, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Delete an exchange
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to delete</param>
        /// <param name="ifUnused">If set, the server will only delete the exchange if it has no queue bindings.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void ExchangeDelete(this IAdvancedBus bus, Exchange exchange, bool ifUnused = false, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.ExchangeDeleteAsync(exchange, ifUnused, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        private class SimpleConsumeConfiguration : ISimpleConsumeConfiguration
        {
            public string ConsumerTag { get; private set; }
            public bool? IsExclusive { get; private set; }
            public ushort? PrefetchCount { get; private set; }
            public IDictionary<string, object> Arguments { get; private set; }

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
