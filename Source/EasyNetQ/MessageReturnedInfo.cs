namespace EasyNetQ
{
    public class MessageReturnedInfo
    {
        public string Exchange { get; }
        public string RoutingKey { get; }
        public string ReturnReason { get; }

        public MessageReturnedInfo(
            string exchange,
            string routingKey,
            string returnReason
        )
        {
            Preconditions.CheckNotNull(exchange, nameof(exchange));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(returnReason, nameof(returnReason));

            Exchange = exchange;
            RoutingKey = routingKey;
            ReturnReason = returnReason;
        }
    }
}
