namespace EasyNetQ
{
    public class MessageReturnedInfo
    {
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string ReturnReason { get; set; }

        public MessageReturnedInfo(
            string exchange, 
            string routingKey, 
            string returnReason)
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