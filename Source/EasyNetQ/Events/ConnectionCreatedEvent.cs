using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after an initial connection to the endpoint
    /// </summary>
    public class ConnectionCreatedEvent
    {
        /// <summary>
        ///     The endpoint a connection is connected to
        /// </summary>
        public AmqpTcpEndpoint Endpoint { get; }

        /// <summary>
        ///     Creates ConnectionCreatedEvent
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        public ConnectionCreatedEvent(AmqpTcpEndpoint endpoint)
        {
            Endpoint = endpoint;
        }
    }
}
