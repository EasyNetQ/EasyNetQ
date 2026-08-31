using System.Collections.Concurrent;

namespace EasyNetQ;

/// <summary>
///     The single remaining runtime-reflection escape hatch: creates a <see cref="MessageTypeDescriptor{T}" /> for a
///     <see cref="Type" /> only known at runtime (non-generic APIs, polymorphic bodies, unregistered consumed types).
///     Generic call sites never come here. The source generator (Phase 3) pre-registers discoverable types, making
///     this unreachable in AOT-compatible applications.
/// </summary>
internal static class RuntimeDescriptorFactory
{
    private static readonly ConcurrentDictionary<Type, Func<string, MessageTypeDescriptor>> Factories = new();

    public static MessageTypeDescriptor Create(Type type, string wireName)
    {
        var factory = Factories.GetOrAdd(type, static t =>
        {
            var descriptorType = typeof(MessageTypeDescriptor<>).MakeGenericType(t);
            var constructor = descriptorType.GetConstructors(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            ).Single();
            return name => (MessageTypeDescriptor)constructor.Invoke([name]);
        });
        return factory(wireName);
    }
}
