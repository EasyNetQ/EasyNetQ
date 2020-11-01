using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a successful connection to the endpoint
    /// </summary>
    public class ConnectionDisconnectedEvent
    {
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
        /// <param name="endpoint">The endpoint</param>
        /// <param name="reason">The reason</param>
        public ConnectionDisconnectedEvent(AmqpTcpEndpoint endpoint, string reason)
        {
            Endpoint = endpoint;
            Reason = reason;
        }
    }
}
