namespace EasyNetQ.Events
{
    public class PublishedMessageEvent
    {
        public string ExchangeName { get; private set; }
        public string RoutingKey { get; private set; }
        public MessageProperties Properties { get; private set; }
        public byte[] Body { get; private set; }

        public PublishedMessageEvent(string exchangeName, string routingKey, MessageProperties properties, byte[] body)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            Properties = properties;
            Body = body;
        }
    }
}