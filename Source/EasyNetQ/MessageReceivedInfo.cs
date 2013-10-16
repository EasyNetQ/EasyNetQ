namespace EasyNetQ
{
    public class MessageReceivedInfo
    {
        public string ConsumerTag { get; private set; }
        public ulong DeliverTag { get; private set; }
        public bool Redelivered { get; private set; }
        public string Exchange { get; private set; }
        public string RoutingKey { get; private set; }
        public string Queue { get; private set; }

        public MessageReceivedInfo(
            string consumerTag, 
            ulong deliverTag, 
            bool redelivered, 
            string exchange, 
            string routingKey,
            string queue)
        {
            Preconditions.CheckNotNull(consumerTag, "consumerTag");
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(routingKey, "routingKey");
            Preconditions.CheckNotNull(queue, "queue");

            ConsumerTag = consumerTag;
            DeliverTag = deliverTag;
            Redelivered = redelivered;
            Exchange = exchange;
            RoutingKey = routingKey;
            Queue = queue;
        }
    }
}