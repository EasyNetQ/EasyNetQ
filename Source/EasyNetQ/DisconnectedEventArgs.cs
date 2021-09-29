using System;
using EasyNetQ.Persistent;

namespace EasyNetQ
{
    /// <summary>
    ///     Arguments of Disconnected event
    /// </summary>
    public class DisconnectedEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates DisconnectedEventArgs
        /// </summary>
        public DisconnectedEventArgs(
            PersistentConnectionType type, string hostname, int port, string reason
        )
        {
            Type = type;
            Hostname = hostname;
            Port = port;
            Reason = reason;
        }

        /// <summary>
        ///     The type of the associated connection
        /// </summary>
        public PersistentConnectionType Type { get; }

        /// <summary>
        ///     The hostname of the disconnected endpoint
        /// </summary>
        public string Hostname { get; }

        /// <summary>
        ///     The port of the disconnected endpoint
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     The reason of the disconnection
        /// </summary>
        public string Reason { get; }
    }
}
