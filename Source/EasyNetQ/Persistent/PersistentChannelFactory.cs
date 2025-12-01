using EasyNetQ.Logging;

namespace EasyNetQ.Persistent;

/// <inheritdoc />
public class PersistentChannelFactory : IPersistentChannelFactory
{
    private readonly ILogger<PersistentChannel> logger;
    private readonly IEventBus eventBus;

    /// <summary>
    ///    Creates PersistentChannelFactory
    /// </summary>
    public PersistentChannelFactory(ILogger<PersistentChannel> logger, IEventBus eventBus)
    {
        this.logger = logger;
        this.eventBus = eventBus;
    }

    /// <inheritdoc />
    public IPersistentChannel CreatePersistentChannel(
        IPersistentConnection connection, PersistentChannelOptions options
    )
    {
        return new PersistentChannel(options, logger, connection, eventBus);
    }
}
