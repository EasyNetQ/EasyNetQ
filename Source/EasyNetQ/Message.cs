namespace EasyNetQ
{
    public class Message<T> : IMessage<T> where T : class
    {
        public MessageProperties Properties { get; private set; }
        public T Body { get; private set; }

        public Message(T body)
        {
            Preconditions.CheckNotNull(body, "body");

            Properties = new MessageProperties();
            Body = body;
        }

        public void SetProperties(MessageProperties properties)
        {
            Preconditions.CheckNotNull(properties, "properties");
            Properties = properties;
        }
    }
}