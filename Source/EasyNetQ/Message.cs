namespace EasyNetQ
{
    public class Message<T> : IMessage<T>
    {
        public MessageProperties Properties { get; private set; }
        public T Body { get; private set; }
        public string ConsumerTag { get; set; }
        public ulong DeliverTag { get; set; }
        public bool Redelivered { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }

        public Message(T body)
        {
            Properties = new MessageProperties();
            Body = body;
        }
    }
}