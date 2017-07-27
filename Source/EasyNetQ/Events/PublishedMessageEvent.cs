namespace EasyNetQ.Events
{
    public class PublishedMessageEvent
    {
        public string ExchangeName { get; }
        public string RoutingKey { get; }
        public MessageProperties Properties { get; }
        public byte[] Body { get; }

        public PublishedMessageEvent(string exchangeName, string routingKey, MessageProperties properties, byte[] body)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            Properties = properties;
            Body = body;
        }
    }
}