using System;

namespace EasyNetQ
{
    /// <summary>
    ///     The arguments of Blocked event
    /// </summary>
    public class BlockedEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates BlockedEventArgs
        /// </summary>
        /// <param name="reason">The reason</param>
        public BlockedEventArgs(string reason)
        {
            Reason = reason;
        }

        /// <summary>
        ///     The reason of the blocking
        /// </summary>
        public string Reason { get; }
    }
}
