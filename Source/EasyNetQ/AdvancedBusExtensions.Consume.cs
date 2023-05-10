using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IAdvancedBus"/>
/// </summary>
public static partial class AdvancedBusExtensions
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
    ) => bus.Consume(queue, handler, _ => { });

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
    ) => bus.Consume(queue, handler, _ => { });

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
    ) => bus.Consume<T>(queue, (m, i, _) => handler(m, i), configure);

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
        => bus.Consume(queue, addHandlers, _ => { });

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
    ) => bus.Consume(queue, handler, _ => { });

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
    ) => bus.Consume(queue, handler, _ => { });

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
    ) => bus.Consume(queue, handler, _ => { });

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
    ) => bus.Consume(queue, (m, p, i, _) => handler(m, p, i), configure);

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
    ) => bus.Consume(queue, (m, p, i, _) => handler(m, p, i), configure);

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
    public static IDisposable Consume(this IAdvancedBus bus, Queue queue, MessageHandler handler)
        => bus.Consume(queue, handler, _ => { });

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
    /// <returns>A disposable to cancel the consumer</returns>
    public static IDisposable Consume(
        this IAdvancedBus bus,
        Queue queue,
        Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, CancellationToken, Task> handler
    ) => bus.Consume(queue, handler, _ => { });


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

    private class SimpleConsumeConfiguration : ISimpleConsumeConfiguration
    {
        public bool AutoAck { get; private set; }
        public string? ConsumerTag { get; private set; }
        public bool? IsExclusive { get; private set; }
        public ushort? PrefetchCount { get; private set; }
        public IDictionary<string, object>? Arguments { get; private set; }

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
