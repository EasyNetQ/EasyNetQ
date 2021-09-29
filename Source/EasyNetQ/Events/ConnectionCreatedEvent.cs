using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after an initial connection to the endpoint
    /// </summary>
    public readonly struct ConnectionCreatedEvent
    {
        /// <summary>
        ///     The type of the associated connection
        /// </summary>
        public PersistentConnectionType Type { get; }

        /// <summary>
        ///     The endpoint a connection is connected to
        /// </summary>
        public AmqpTcpEndpoint Endpoint { get; }

        /// <summary>
        ///     Creates ConnectionCreatedEvent
        /// </summary>
        public ConnectionCreatedEvent(PersistentConnectionType type, AmqpTcpEndpoint endpoint)
        {
            Type = type;
            Endpoint = endpoint;
        }
    }
}
