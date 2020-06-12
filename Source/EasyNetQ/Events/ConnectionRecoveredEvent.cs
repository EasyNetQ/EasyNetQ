using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a recovery of the connection to the endpoint
    /// </summary>
    public class ConnectionRecoveredEvent
    {
        /// <summary>
        ///     The endpoint a connection is connected to
        /// </summary>
        public AmqpTcpEndpoint Endpoint { get; }

        /// <summary>
        ///     Creates ConnectionRecoveredEvent
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        public ConnectionRecoveredEvent(AmqpTcpEndpoint endpoint)
        {
            Endpoint = endpoint;
        }
    }
}
