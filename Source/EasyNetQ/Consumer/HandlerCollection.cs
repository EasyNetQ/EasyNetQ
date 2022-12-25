using System.Collections.Concurrent;

namespace EasyNetQ.Consumer;

/// <inheritdoc />
public class HandlerCollection : IHandlerCollection
{
    private readonly ConcurrentDictionary<Type, IMessageHandler> handlers = new();

    /// <inheritdoc />
    public IHandlerRegistration Add<T>(IMessageHandler<T> handler)
    {
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

        return (_, _, _) => Task.FromResult(AckStrategies.Ack);
    }

    /// <inheritdoc />
    public bool ThrowOnNoMatchingHandler { get; set; } = true;
}
