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
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(routingKey, "routingKey");
            Preconditions.CheckNotNull(returnReason, "returnReason");

            Exchange = exchange;
            RoutingKey = routingKey;
            ReturnReason = returnReason;
        }
    }
}
