using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IPerQueueConsumeConfiguration"/>
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
        => configuration.WithArgument("x-priority", priority);

    /// <summary>
    ///     Adds arguments
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="arguments">The arguments to add</param>
    /// <returns>IPerQueueConsumeConfiguration</returns>
    public static IPerQueueConsumeConfiguration WithArguments(
        this IPerQueueConsumeConfiguration configuration, IDictionary<string, object> arguments
    ) => arguments.Aggregate(configuration, (c, kvp) => c.WithArgument(kvp.Key, kvp.Value));
}

/// <summary>
///     Various extensions for <see cref="IConsumeConfiguration"/>
/// </summary>
public static class ConsumeConfigurationExtensions
{
    public static IConsumeConfiguration ForQueue(
        this IConsumeConfiguration configuration,
        in Queue queue,
        MessageHandler handler
    ) => configuration.ForQueue(queue, handler, _ => { });

    public static IConsumeConfiguration ForQueue(
        this IConsumeConfiguration configuration,
        in Queue queue,
        Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, CancellationToken, Task> handler
    ) => configuration.ForQueue(queue, handler, _ => { });

    public static IConsumeConfiguration ForQueue(
        this IConsumeConfiguration configuration,
        in Queue queue,
        Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, CancellationToken, Task> handler,
        Action<IPerQueueConsumeConfiguration> configure
    )
    {
        return configuration.ForQueue(
            queue,
            async (body, properties, receivedInfo, cancellationToken) =>
            {
                await handler(body, properties, receivedInfo, cancellationToken).ConfigureAwait(false);
                return AckDecision.Ack;
            },
            configure
        );
    }

    public static IConsumeConfiguration ForQueue<T>(
        this IConsumeConfiguration configuration,
        in Queue queue,
        IMessageHandler<T> handler
    ) => configuration.ForQueue(queue, handler, _ => { });

    public static IConsumeConfiguration ForQueue<T>(
        this IConsumeConfiguration configuration,
        in Queue queue,
        IMessageHandler<T> handler,
        Action<IPerQueueConsumeConfiguration> configure
    ) => configuration.ForQueue(queue, x => x.Add(handler), configure);

    /// <summary>
    ///     Adds a handler for the deserialized body (no IMessage envelope is allocated)
    /// </summary>
    public static IConsumeConfiguration ForQueue<T>(
        this IConsumeConfiguration configuration,
        in Queue queue,
        MessageHandler<T> handler
    ) => configuration.ForQueue(queue, handler, _ => { });

    /// <summary>
    ///     Adds a handler for the deserialized body (no IMessage envelope is allocated)
    /// </summary>
    public static IConsumeConfiguration ForQueue<T>(
        this IConsumeConfiguration configuration,
        in Queue queue,
        MessageHandler<T> handler,
        Action<IPerQueueConsumeConfiguration> configure
    ) => configuration.ForQueue(
        queue,
        x =>
        {
            if (x is HandlerCollection handlerCollection)
                handlerCollection.Add(handler);
            else
                throw new NotSupportedException(
                    "Body handlers require the built-in HandlerCollection; register an IMessage-based handler with this custom IHandlerCollectionFactory"
                );
        },
        configure
    );

    public static IConsumeConfiguration ForQueue<T>(
        this IConsumeConfiguration configuration,
        in Queue queue,
        Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> handler
    ) => configuration.ForQueue(queue, handler, _ => { });

    public static IConsumeConfiguration ForQueue<T>(
        this IConsumeConfiguration configuration,
        in Queue queue,
        Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> handler,
        Action<IPerQueueConsumeConfiguration> configure
    ) => configuration.ForQueue(queue, x => x.Add(handler), configure);

    public static IConsumeConfiguration ForQueue<T>(
        this IConsumeConfiguration configuration,
        in Queue queue,
        Action<IMessage<T>, MessageReceivedInfo> handler
    ) => configuration.ForQueue(queue, handler, _ => { });

    public static IConsumeConfiguration ForQueue<T>(
        this IConsumeConfiguration configuration,
        in Queue queue,
        Action<IMessage<T>, MessageReceivedInfo> handler,
        Action<IPerQueueConsumeConfiguration> configure
    ) => configuration.ForQueue(queue, x => x.Add(handler), configure);
}
