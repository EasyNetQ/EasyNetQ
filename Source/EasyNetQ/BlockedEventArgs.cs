using System;

namespace EasyNetQ
{
    /// <summary>
    ///     Represents queue stats
    /// </summary>
    public readonly struct QueueStats
    {
        /// <summary>
        ///     Creates QueueStats
        /// </summary>
        /// <param name="messagesCount">The messages count</param>
        /// <param name="consumersCount">The consumers count</param>
        public QueueStats(ulong messagesCount, ulong consumersCount)
        {
            MessagesCount = messagesCount;
            ConsumersCount = consumersCount;
        }

        /// <summary>
        ///     Messages count
        /// </summary>
        public ulong MessagesCount { get; }

        /// <summary>
        ///     Consumers count
        /// </summary>
        public ulong ConsumersCount { get; }
    }

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
