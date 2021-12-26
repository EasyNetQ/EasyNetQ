using EasyNetQ.Logging;
using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.Consumer;

/// <inheritdoc />
public interface IConsumerConnection : IPersistentConnection
{
}

/// <inheritdoc cref="PersistentConnection" />
public sealed class ConsumerConnection : PersistentConnection, IConsumerConnection
{
    /// <inheritdoc />
    public ConsumerConnection(
        ILogger<ConsumerConnection> logger,
        ConnectionConfiguration configuration,
        IConnectionFactory connectionFactory,
        IEventBus eventBus
    ) : base(PersistentConnectionType.Consumer, logger, configuration, connectionFactory, eventBus)
    {
    }
}
