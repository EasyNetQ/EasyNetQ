using System;

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

    public static class Message
    {
        /// <summary>
        /// Create a message instance when you only have a runtime type to play with.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="body">The message body as a .NET type</param>
        /// <returns></returns>
        public static dynamic CreateInstance(Type messageType, object body)
        {
            var constructor = typeof (Message<>).MakeGenericType(messageType).GetConstructor(new[] {messageType});
// ReSharper disable PossibleNullReferenceException
            return constructor.Invoke(new[] {body});
// ReSharper restore PossibleNullReferenceException
        }
    }
}