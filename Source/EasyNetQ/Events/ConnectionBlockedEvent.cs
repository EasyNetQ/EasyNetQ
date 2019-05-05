namespace EasyNetQ.Events
{
    public struct ConnectionBlockedEvent
    {
        public string Reason { get; }

        public ConnectionBlockedEvent(string reason)
        {
            Reason = reason;
        }
    }
}
