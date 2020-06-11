using System;

namespace EasyNetQ
{
    /// <summary>
    ///     Arguments of Connected event
    /// </summary>
    public class ConnectedEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates ConnectedEventArgs
        /// </summary>
        /// <param name="hostname">The hostname</param>
        /// <param name="port">The port</param>
        public ConnectedEventArgs(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }

        /// <summary>
        ///     The hostname of the connected endpoint
        /// </summary>
        public string Hostname { get; }

        /// <summary>
        ///     The port of the connected endpoint
        /// </summary>
        public int Port { get; }
    }
}
