using System;

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
        /// <param name="hostname">The hostname</param>
        /// <param name="port">The port</param>
        /// <param name="reason">The reason</param>
        public DisconnectedEventArgs(string hostname, int port, string reason)
        {
            Hostname = hostname;
            Port = port;
            Reason = reason;
        }

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
