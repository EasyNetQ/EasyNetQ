using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a recovery of the connection to the endpoint
/// </summary>
/// <param name="Type">The type of the associated connection</param>
/// <param name="Endpoint">The The endpoint a connection is connected to</param>
public readonly record struct ConnectionRecoveredEvent(PersistentConnectionType Type, AmqpTcpEndpoint Endpoint);
