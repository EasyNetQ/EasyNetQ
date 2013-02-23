namespace EasyNetQ
{
    public class Message : IMessage
    {
        public MessageProperties Properties { get; private set; }
        public object Body { get; private set; }

        public Message(object body)
        {
            Properties = new MessageProperties();
            Body = body;
        }

        public void SetProperties(MessageProperties properties)
        {
            Properties = properties;
        }
    }

    public class Message<T> : Message, IMessage<T>
    {
        public Message(T body) : base(body)
        {
        }

        public T GetBody()
        {
            return (T) Body;
        }
    }
}