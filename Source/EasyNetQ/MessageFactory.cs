using System;
using System.Collections.Concurrent;

namespace EasyNetQ
{
    /// <summary>
    /// Creates a generic <see cref="IMessage{T}"/> and returns it casted as <see cref="IMessage"/>
    /// so it can be used in scenarios where we only have a runtime <see cref="Type"/> available.
    /// </summary>
    public static class MessageFactory
    {
        private static readonly ConcurrentDictionary<Type, Type> genericMessageTypesMap = new();

        public static IMessage CreateInstance(Type messageType, object body)
        {
            Preconditions.CheckNotNull(messageType, nameof(messageType));
            Preconditions.CheckNotNull(body, nameof(body));

            var genericType = genericMessageTypesMap.GetOrAdd(messageType, t => typeof(Message<>).MakeGenericType(t));
            return (IMessage)Activator.CreateInstance(genericType, body);
        }

        public static IMessage CreateInstance(Type messageType, object body, MessageProperties properties)
        {
            Preconditions.CheckNotNull(messageType, nameof(messageType));
            Preconditions.CheckNotNull(properties, nameof(properties));

            var genericType = genericMessageTypesMap.GetOrAdd(messageType, t => typeof(Message<>).MakeGenericType(t));
            return (IMessage)Activator.CreateInstance(genericType, body, properties);
        }
    }
}
