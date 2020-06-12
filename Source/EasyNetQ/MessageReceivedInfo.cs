namespace EasyNetQ
{
    public class MessageReceivedInfo
    {
        public string ConsumerTag { get; set; }
        public ulong DeliveryTag { get; set; }
        public bool Redelivered { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string Queue { get; set; }

        public MessageReceivedInfo() { }

        public MessageReceivedInfo(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            string queue
        )
        {
            Preconditions.CheckNotNull(consumerTag, "consumerTag");
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(routingKey, "routingKey");
            Preconditions.CheckNotNull(queue, "queue");

            ConsumerTag = consumerTag;
            DeliveryTag = deliveryTag;
            Redelivered = redelivered;
            Exchange = exchange;
            RoutingKey = routingKey;
            Queue = queue;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[ConsumerTag={ConsumerTag}, DeliveryTag={DeliveryTag}, Redelivered={Redelivered}, Exchange={Exchange}, RoutingKey={RoutingKey}, Queue={Queue}]";
        }
    }
}
