using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    public class ReturnedMessageEvent
    {
        public IModel Channel { get; }
        public byte[] Body { get; }
        public MessageProperties Properties { get; }
        public MessageReturnedInfo Info { get; }

        public ReturnedMessageEvent(IModel channel, byte[] body, MessageProperties properties, MessageReturnedInfo info)
        {
            Channel = channel;
            Body = body;
            Properties = properties;
            Info = info;
        }
    }
}
