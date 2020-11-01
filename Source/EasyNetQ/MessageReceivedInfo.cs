namespace EasyNetQ
{
    /// <summary>
    ///     Represents various properties of a received message
    /// </summary>
    public class MessageReceivedInfo
    {
        /// <summary>
        ///     Consumer tag
        /// </summary>
        public string ConsumerTag { get; }

        /// <summary>
        ///     Delivery tag
        /// </summary>
        public ulong DeliveryTag { get; }

        /// <summary>
        ///     True if a message is redelivered
        /// </summary>
        public bool Redelivered { get; }

        /// <summary>
        ///     Exchange
        /// </summary>
        public string Exchange { get; }

        /// <summary>
        ///     Routing key
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     Queue
        /// </summary>
        public string Queue { get; }

        /// <summary>
        ///     Creates MessageReceivedInfo
        /// </summary>
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
