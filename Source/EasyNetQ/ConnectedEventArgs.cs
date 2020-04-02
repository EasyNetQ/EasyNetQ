using System;

namespace EasyNetQ
{
    /// <summary>
    /// Event arguments for <see cref="IAdvancedBus.Connected"/> event.
    /// </summary>
    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }

        /// <summary>
        /// The hostname of the broker endpoind being connected to.
        /// </summary>
        public string Hostname { get; }
        /// <summary>
        /// The port number of the broker endpoind being connected to.
        /// </summary>
        public int Port { get; }
    }
}
