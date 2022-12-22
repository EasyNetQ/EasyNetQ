using EasyNetQ.Logging;

namespace EasyNetQ.Persistent;

/// <inheritdoc />
public class PersistentChannelFactory : IPersistentChannelFactory
{
    private readonly IEventBus eventBus;
    private readonly ILogger<PersistentChannel> logger;

    /// <summary>
    ///    Creates PersistentChannelFactory
    /// </summary>
    public PersistentChannelFactory(IEventBus eventBus, ILogger<PersistentChannel> logger = null)
    {
        Preconditions.CheckNotNull(eventBus, nameof(eventBus));

        this.eventBus = eventBus;
        this.logger = logger;
    }

    /// <inheritdoc />
    public IPersistentChannel CreatePersistentChannel(
        IPersistentConnection connection, PersistentChannelOptions options
    )
    {
        return new PersistentChannel(options, connection, eventBus, logger);
    }
}
