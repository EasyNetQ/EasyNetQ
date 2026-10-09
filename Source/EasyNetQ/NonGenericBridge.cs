using System.Collections.Concurrent;

namespace EasyNetQ;

/// <summary>
///     Descriptor cache for the non-generic messaging APIs. Descriptors act as typed trampolines into the generic
///     APIs; for runtime types they are created through <see cref="RuntimeDescriptorFactory" /> (once per type),
///     which is why the non-generic APIs carry [RequiresDynamicCode].
/// </summary>
internal static class NonGenericBridge
{
    internal const string RequiresDynamicCodeMessage =
        "Non-generic messaging APIs close generic methods over runtime types. Use the generic APIs for AOT-compatible applications.";

    private static readonly ConcurrentDictionary<Type, MessageTypeDescriptor> Descriptors = new();

    public static MessageTypeDescriptor Get(Type messageType)
        => Descriptors.GetOrAdd(messageType, static t => RuntimeDescriptorFactory.Create(t, t.FullName ?? t.Name));
}
