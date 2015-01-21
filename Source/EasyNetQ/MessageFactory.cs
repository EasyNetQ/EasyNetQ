using System;

namespace EasyNetQ
{
    /// <summary>
    /// Creates a generic <see cref="IMessage{T}"/> and returns it casted as <see cref="IMessage"/>
    /// so it can be used in scenarios where we only have a runtime <see cref="Type"/> available. 
    /// </summary>
    public static class MessageFactory
    {
        public static IMessage CreateInstance(Type messageType, object body)
        {
            var constructor = typeof(Message<>).MakeGenericType(messageType).GetConstructor(new[] { messageType });
// ReSharper disable PossibleNullReferenceException
            var message = constructor.Invoke(new[] { body }) as IMessage;
            return (IMessage)message;
// ReSharper restore PossibleNullReferenceException
        }

        public static IMessage CreateInstance(Type messageType, object body, MessageProperties properties)
        {
            var constructor = typeof(Message<>).MakeGenericType(messageType).GetConstructor(new[] { messageType, typeof(MessageProperties) });
// ReSharper disable PossibleNullReferenceException
            var message = constructor.Invoke(new[] { body, properties });
            return (IMessage)message;
// ReSharper restore PossibleNullReferenceException
        }
    }
}