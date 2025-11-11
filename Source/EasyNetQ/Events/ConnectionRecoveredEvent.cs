using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a recovery of the connection to the endpoint
/// </summary>
public readonly struct ConnectionRecoveredEvent
{
    /// <summary>
    ///     The type of a connection
    /// </summary>
    public PersistentConnectionType Type { get; }

    /// <summary>
    ///     The endpoint a connection is connected to
    /// </summary>
    public AmqpTcpEndpoint Endpoint { get; }

    /// <summary>
    ///     Creates ConnectionRecoveredEvent
    /// </summary>
    public ConnectionRecoveredEvent(PersistentConnectionType type, AmqpTcpEndpoint endpoint)
    {
        Type = type;
        Endpoint = endpoint;
    }
}
