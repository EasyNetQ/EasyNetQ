namespace EasyNetQ.Events
{
    public class ConnectionDisconnectedEvent
    {
        public ConnectionDisconnectedEvent(string hostname, int port, string reasonText)
        {
            Hostname = hostname;
            Port = port;
            ReasonText = reasonText;
        }
        public string Hostname { get; }
        public int Port { get; }
        public string ReasonText { get; }
    }
}
