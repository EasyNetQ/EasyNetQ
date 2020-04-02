using System;

namespace EasyNetQ
{
    /// <summary>
    /// Event arguments for <see cref="IAdvancedBus.Disconnected"/> event.
    /// </summary>
    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedEventArgs(string hostname, int port, string reason)
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
