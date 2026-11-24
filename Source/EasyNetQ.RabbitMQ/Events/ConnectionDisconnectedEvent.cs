using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a successful connection to the endpoint
/// </summary>
/// <param name="Type">The type of the associated connection</param>
/// <param name="Endpoint">The The endpoint a connection is connected to</param>
/// <param name="Reason">The reason of a disconnection</param>
public readonly record struct ConnectionDisconnectedEvent(
    PersistentConnectionType Type,
    AmqpTcpEndpoint Endpoint,
    string Reason
);
