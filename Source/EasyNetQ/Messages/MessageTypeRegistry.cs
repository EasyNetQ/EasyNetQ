using System.Collections.Concurrent;

namespace EasyNetQ;

/// <inheritdoc />
public sealed class MessageTypeRegistry : IMessageTypeRegistry
{
    private readonly ConcurrentDictionary<Type, MessageTypeDescriptor> byType = new();
    private readonly ConcurrentDictionary<string, MessageTypeDescriptor> byWireName = new();
    private readonly ITypeNameSerializer typeNameSerializer;

    /// <summary>
    ///     Creates the registry. Wire names come from the configured <see cref="ITypeNameSerializer" /> so they stay
    ///     identical to the names 8.x peers produce and expect (including UseLegacyTypeNaming).
    /// </summary>
    public MessageTypeRegistry(ITypeNameSerializer typeNameSerializer)
        : this(typeNameSerializer, null)
    {
    }

    /// <summary>
    ///     Creates the registry and applies generated initializers, closing every discoverable message type at
    ///     construction so steady-state lookups never fall back to <see cref="RuntimeDescriptorFactory" />.
    /// </summary>
    public MessageTypeRegistry(ITypeNameSerializer typeNameSerializer, IEnumerable<IMessageTypeRegistryInitializer>? initializers)
    {
        this.typeNameSerializer = typeNameSerializer;
        if (initializers is null) return;
        foreach (var initializer in initializers)
            initializer.Initialize(this);
    }

    /// <inheritdoc />
    public MessageTypeDescriptor<T> GetOrAdd<T>()
    {
        return byType.TryGetValue(typeof(T), out var existing)
            ? (MessageTypeDescriptor<T>)existing
            : (MessageTypeDescriptor<T>)Register(Populate(new MessageTypeDescriptor<T>(typeNameSerializer.Serialize(typeof(T)))));
    }

    /// <inheritdoc />
    public MessageTypeDescriptor GetOrAdd(Type type)
    {
        return byType.TryGetValue(type, out var existing)
            ? existing
            : Register(Populate(RuntimeDescriptorFactory.Create(type, typeNameSerializer.Serialize(type))));
    }

    /// <inheritdoc />
    public bool TryGetByWireName(string wireName, out MessageTypeDescriptor descriptor)
        => byWireName.TryGetValue(wireName, out descriptor!);

    /// <inheritdoc />
    public MessageTypeDescriptor GetByWireName(string wireName)
    {
        if (byWireName.TryGetValue(wireName, out var existing))
            return existing;

        // Unknown wire name: resolve the CLR type through the type name serializer (runtime fallback; the source
        // generator will pre-register every discoverable type so this path disappears for AOT-compatible apps).
        var descriptor = GetOrAdd(typeNameSerializer.Deserialize(wireName));
        // cache under the incoming name too, which may differ from descriptor.WireName for legacy formats
        byWireName.TryAdd(wireName, descriptor);
        return descriptor;
    }

    private static TDescriptor Populate<TDescriptor>(TDescriptor descriptor) where TDescriptor : MessageTypeDescriptor
    {
        AttributeMetadataReader.Populate(descriptor);
        return descriptor;
    }

    private MessageTypeDescriptor Register(MessageTypeDescriptor descriptor)
    {
        var registered = byType.GetOrAdd(descriptor.Type, descriptor);
        byWireName.TryAdd(registered.WireName, registered);
        return registered;
    }
}
