namespace EasyNetQ
{
    public class Message<T> : IMessage<T>
    {
        public MessageProperties Properties { get; private set; }
        public T Body { get; private set; }

        public Message(T body)
        {
            Properties = new MessageProperties();
            Body = body;
        }

        public void SetProperties(MessageProperties properties)
        {
            Properties = properties;
        }
    }
}