namespace EasyNetQ.InMemoryClient
{
    public class BindingInfo
    {
        public QueueInfo Queue { get; private set; }
        public string RoutingKey { get; private set; }

        public BindingInfo(QueueInfo queue, string routingKey)
        {
            Queue = queue;
            RoutingKey = routingKey;
        }

        public bool RoutingKeyMatches(string messageRouting)
        {
            if (RoutingKey == "#") return true;
            if (messageRouting == RoutingKey) return true;
            return false;
        }
    }
}