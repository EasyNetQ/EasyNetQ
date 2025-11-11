using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a successful connection to the endpoint
/// </summary>
public readonly struct ConnectionDisconnectedEvent
{
    /// <summary>
    ///     The type of the associated connection
    /// </summary>
    public PersistentConnectionType Type { get; }

    /// <summary>
    ///     The endpoint a connection is disconnected from
    /// </summary>
    public AmqpTcpEndpoint Endpoint { get; }

    /// <summary>
    ///     The reason of a disconnection
    /// </summary>
    public string Reason { get; }

    /// <summary>
    ///     Creates ConnectionDisconnectedEvent
    /// </summary>
    public ConnectionDisconnectedEvent(
        PersistentConnectionType type,
        AmqpTcpEndpoint endpoint,
        string reason
    )
    {
        Type = type;
        Endpoint = endpoint;
        Reason = reason;
    }
}
