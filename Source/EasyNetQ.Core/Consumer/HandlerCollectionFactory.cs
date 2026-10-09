using EasyNetQ.Topology;

namespace EasyNetQ.Consumer;

public class HandlerCollectionFactory : IHandlerCollectionFactory
{
    private readonly IMessageTypeRegistry registry;

    public HandlerCollectionFactory(IMessageTypeRegistry registry)
    {
        this.registry = registry;
    }

    /// <inheritdoc />
    public IHandlerCollection CreateHandlerCollection(in Queue queue) => new HandlerCollection(registry);
}
