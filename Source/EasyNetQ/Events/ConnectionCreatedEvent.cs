namespace EasyNetQ.Events
{
    /// <summary>
    /// <see cref="IEventBus"/> event for when connection has been creaded.
    /// </summary>
    public class ConnectionCreatedEvent
    {
        public ConnectionCreatedEvent(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }
        /// <summary>
        /// Hostname for the disconnected connection 
        /// </summary>
        public string Hostname { get; }
        /// <summary>
        /// Port number for the disconnected connection 
        /// </summary>
        public int Port { get; }
    }
}
