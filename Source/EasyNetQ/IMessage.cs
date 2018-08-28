using System;

namespace EasyNetQ
{
    public interface IMessage<out T> : IMessage
    {
        /// <summary>
        /// The message body as a .NET type.
        /// This will return the same underlying object than <see cref="IMessage.GetBody"/> but will be strongly typed.
        /// </summary>
        T Body { get; }
    }

    public interface IMessage
    {
        /// <summary>
        /// The message properties.
        /// </summary>
        MessageProperties Properties { get; }
        /// <summary>
        /// The message body return as an object when we only have runtime types and can't use generics.
        /// </summary>
        object GetBody();
        /// <summary>
        /// The message <see cref="Type"/>. This is a shortcut to GetBody().GetType().
        /// </summary>
        Type MessageType { get; }
    }

    public class Message<T> : IMessage<T>
    {
        public MessageProperties Properties { get; private set; }
        public Type MessageType { get; }
        public T Body { get; }

        public object GetBody() { return Body; }

        public Message(T body)
        {
            Preconditions.CheckNotNull(body, "body");
            Body = body;
            Properties = new MessageProperties();
            MessageType = body.GetType();
        }

        public Message(T body, MessageProperties properties)
        {
            Preconditions.CheckNotNull(body, "body");
            Preconditions.CheckNotNull(properties, "properties");
            Body = body;
            Properties = properties;
            MessageType = body.GetType();
        }

        public void SetProperties(MessageProperties properties)
        {
            Preconditions.CheckNotNull(properties, "properties");
            Properties = properties;
        }
    }
}