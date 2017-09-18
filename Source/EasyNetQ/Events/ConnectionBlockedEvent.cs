namespace EasyNetQ.Events
{
    public class ConnectionBlockedEvent
    {
        public string Reason { get; }

        public ConnectionBlockedEvent(string reason)
        {
            Reason = reason;
        }
    }
}