using System.Collections.Concurrent;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer;

/// <inheritdoc />
public class HandlerCollectionPerQueueFactory : IHandlerCollectionFactory
{
    private readonly ConcurrentDictionary<string, IHandlerCollection> handlerCollections = new();
    private readonly IMessageTypeRegistry registry;

    public HandlerCollectionPerQueueFactory(IMessageTypeRegistry registry)
    {
        this.registry = registry;
    }

    /// <inheritdoc />
    public IHandlerCollection CreateHandlerCollection(in Queue queue) => handlerCollections.GetOrAdd(queue.Name, _ => new HandlerCollection(registry));
}
