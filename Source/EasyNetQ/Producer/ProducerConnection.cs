using MS = Microsoft.Extensions.Logging;
using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.Producer;

/// <summary>
///
/// </summary>
public interface IProducerConnection : IPersistentConnection
{
}

/// <inheritdoc cref="EasyNetQ.Producer.IProducerConnection" />
public sealed class ProducerConnection : PersistentConnection, IProducerConnection
{
    /// <summary>
    ///     Creates ProducerConnection
    /// </summary>
    public ProducerConnection(
        MS.ILogger<ProducerConnection> logger,
        ConnectionConfiguration configuration,
        IConnectionFactory connectionFactory,
        IEventBus eventBus
    ) : base(PersistentConnectionType.Producer, logger, configuration, connectionFactory, eventBus)
    {
    }
}
