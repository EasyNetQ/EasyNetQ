namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a block of the connection
    /// </summary>
    public class ConnectionBlockedEvent
    {
        /// <summary>
        ///     The reason of a block
        /// </summary>
        public string Reason { get; }

        /// <summary>
        ///     Creates ConnectionBlockedEvent
        /// </summary>
        /// <param name="reason">The reason</param>
        public ConnectionBlockedEvent(string reason)
        {
            Reason = reason;
        }
    }
}
