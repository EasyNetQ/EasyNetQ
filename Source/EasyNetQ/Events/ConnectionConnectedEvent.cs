using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a connection to the endpoint
    /// </summary>
    public class ConnectionConnectedEvent
    {
        /// <summary>
        ///     The endpoint a connection is connected to
        /// </summary>
        public AmqpTcpEndpoint Endpoint { get; }

        /// <summary>
        ///     Creates ConnectionCreatedEvent
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        public ConnectionConnectedEvent(AmqpTcpEndpoint endpoint)
        {
            Endpoint = endpoint;
        }
    }
}
