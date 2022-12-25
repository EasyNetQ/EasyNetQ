using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace EasyNetQ;

/// <summary>
/// Creates a generic <see cref="IMessage{T}"/> and returns it casted as <see cref="IMessage"/>
/// so it can be used in scenarios where we only have a runtime <see cref="Type"/> available.
/// </summary>
public static class MessageFactory
{
    private static readonly ConcurrentDictionary<Type, Func<object?, MessageProperties, IMessage>> InstanceActivators = new();

    public static IMessage CreateInstance(Type messageType, object? body, MessageProperties properties)
    {
        var activator = InstanceActivators.GetOrAdd(messageType, bodyType =>
        {
            var genericMessageType = typeof(Message<>).MakeGenericType(bodyType);

            var constructor = genericMessageType.GetConstructor(new[] { bodyType, typeof(MessageProperties) })!;
            var bodyParameter = Expression.Parameter(typeof(object));
            var propertiesParameter = Expression.Parameter(typeof(MessageProperties));

            Expression<Func<object?, MessageProperties, IMessage>> expression =
                Expression.Lambda<Func<object?, MessageProperties, IMessage>>(
                    Expression.New(
                        constructor,
                        Expression.Convert(bodyParameter, bodyType),
                        Expression.Convert(propertiesParameter, typeof(MessageProperties))),
                    bodyParameter, propertiesParameter);

            return expression.Compile();
        });

        return activator(body, properties);
    }
}
