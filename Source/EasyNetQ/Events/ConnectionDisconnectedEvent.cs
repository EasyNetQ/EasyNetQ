namespace EasyNetQ.Events
{
    public class ConnectionDisconnectedEvent
    {
        public ConnectionDisconnectedEvent(string hostname, int port, string reason)
        {
            Hostname = hostname;
            Port = port;
            Reason = reason;
        }
        public string Hostname { get; }
        public int Port { get; }
        public string Reason { get; }
    }
}
