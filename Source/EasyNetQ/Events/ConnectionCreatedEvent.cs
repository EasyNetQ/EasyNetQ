namespace EasyNetQ.Events
{
    public class ConnectionCreatedEvent
    {
        public ConnectionCreatedEvent(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }
        public string Hostname { get; }
        public int Port { get; }
    }
}
