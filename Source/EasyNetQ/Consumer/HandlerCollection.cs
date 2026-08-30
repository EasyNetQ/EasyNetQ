using System.Collections.Concurrent;

namespace EasyNetQ.Consumer;

/// <inheritdoc />
public class HandlerCollection : IHandlerCollection
{
    private readonly ConcurrentDictionary<Type, IMessageHandler> handlers = new();

    /// <summary>
    ///     Creates a handler collection backed by a <see cref="HandlerTable" />
    /// </summary>
    public HandlerCollection(IMessageTypeRegistry registry)
    {
        Table = new HandlerTable(registry);
    }

    /// <summary>
    ///     The table the consume pipeline dispatches through
    /// </summary>
    public HandlerTable Table { get; }

    /// <summary>
    ///     Registers a handler for the deserialized body (no <see cref="IMessage" /> envelope is allocated)
    /// </summary>
    public HandlerCollection Add<T>(MessageHandler<T> handler)
    {
        Table.Add(handler);
        return this;
    }

    /// <inheritdoc />
    public IHandlerRegistration Add<T>(IMessageHandler<T> handler)
    {
        // envelope-style handler: the entry materializes the IMessage<T> the handler expects
        Table.Add<T>((body, context) => handler(new Message<T>(body, context.Properties), context.ReceivedInfo, context.CancellationToken));
        if (!handlers.TryAdd(typeof(T), (m, i, c) => handler((IMessage<T>)m, i, c)))
            throw new EasyNetQException("There is already a handler for message type '{0}'", typeof(T).Name);
        return this;
    }

    /// <inheritdoc />
    public IMessageHandler GetHandler(Type messageType)
    {
        if (handlers.TryGetValue(messageType, out var handler)) return handler;

        // no exact handler match found, so let's see if we can find a handler that
        // handles a supertype of the consumed message.
        foreach (var kvp in handlers)
            if (kvp.Key.IsAssignableFrom(messageType))
                return kvp.Value;

        if (ThrowOnNoMatchingHandler)
            throw new EasyNetQException("No handler found for message type {0}", messageType.Name);

        return static (_, _, _) => new ValueTask<AckDecision>(AckDecision.Ack);
    }

    /// <inheritdoc />
    public bool ThrowOnNoMatchingHandler
    {
        get => Table.ThrowOnNoMatchingHandler;
        set => Table.ThrowOnNoMatchingHandler = value;
    }
}
