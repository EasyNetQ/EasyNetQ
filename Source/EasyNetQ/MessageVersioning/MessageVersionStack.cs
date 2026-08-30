using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace EasyNetQ.MessageVersioning;

public class MessageVersionStack : IEnumerable<Type>
{
    // The ISupersede<> interface walk is pure type metadata, so compute it once per message type - 8.x re-ran the
    // GetInterfaces/GetGenericTypeDefinition scan on every serialized message and every publish
    private static readonly ConcurrentDictionary<Type, Type[]> VersionChains = new();

    private readonly Stack<Type> messageVersions;

    public MessageVersionStack(Type messageType)
    {
        var chain = VersionChains.GetOrAdd(messageType, static t =>
        {
            var stack = ExtractMessageVersions(t);
            var bottomUp = stack.ToArray();
            Array.Reverse(bottomUp); // Stack<T>(IEnumerable) pushes in order, so store bottom-up to reproduce the stack
            return bottomUp;
        });
        messageVersions = new Stack<Type>(chain);
    }

    public Type Pop()
    {
        return messageVersions.Pop();
    }

    public bool IsEmpty()
    {
        return !messageVersions.Any();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<Type> GetEnumerator()
    {
        return messageVersions.GetEnumerator();
    }

    private static Stack<Type> ExtractMessageVersions(Type type)
    {
        var messageVersions = new Stack<Type>();
        messageVersions.Push(type);
        while (true)
        {
            var messageType = messageVersions.Peek();
            var supersededType = GetSupersededType(messageType);

            if (supersededType == null)
                break;

            EnsureVersioningValid(messageType, supersededType);
            messageVersions.Push(supersededType);
        }
        messageVersions.TrimExcess();
        return messageVersions;
    }

    private static Type GetSupersededType(Type type)
    {
        if (type.BaseType == null)
            return null;

        var types = FindSupersedes(type);
        var parentTypes = FindSupersedes(type.BaseType);

        return types.Except(parentTypes).FirstOrDefault();
    }

    private static IEnumerable<Type> FindSupersedes(Type type)
    {
        return type
            .GetInterfaces()
            .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(ISupersede<>))
            .SelectMany(t => t.GetGenericArguments());
    }

    private static void EnsureVersioningValid(Type messageType, Type supersededType)
    {
        if (!messageType.GetTypeInfo().IsSubclassOf(supersededType))
            throw new EasyNetQException("Message cannot supersede a type it is not a subclass of. {0} is not a subclass of {1}", messageType.Name, supersededType.Name);
    }
}
