﻿namespace EasyNetQ.Events
{
    /// <summary>
    /// <see cref="IEventBus"/> event for when disconnected from the broker.
    /// </summary>
    public class ConnectionDisconnectedEvent
    {
        public ConnectionDisconnectedEvent(string hostname, int port, string reason)
        {
            Hostname = hostname;
            Port = port;
            Reason = reason;
        }
        /// <summary>
        /// Hostname for the disconnected connection 
        /// </summary>
        public string Hostname { get; }
        /// <summary>
        /// Port number for the disconnected connection 
        /// </summary>
        public int Port { get; }
        /// <summary>
        /// The reason for the disconnected event.
        /// </summary>
        public string Reason { get; }
    }
}
