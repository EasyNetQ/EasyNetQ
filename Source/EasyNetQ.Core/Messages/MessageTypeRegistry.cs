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
        : this(typeNameSerializer, initializers, null)
    {
    }

    /// <summary>
    ///     Creates the registry, applies the configured <paramref name="mappings" /> (wire names and aliases set
    ///     through <c>MessageType&lt;T&gt;(...)</c>) and then the generated initializers. Mappings go first so a
    ///     configured wire name is in place before anything registers the type under its default name.
    /// </summary>
    public MessageTypeRegistry(
        ITypeNameSerializer typeNameSerializer,
        IEnumerable<IMessageTypeRegistryInitializer>? initializers,
        IEnumerable<MessageTypeMapping>? mappings
    )
    {
        this.typeNameSerializer = typeNameSerializer;
        if (mappings is not null)
            foreach (var mapping in mappings)
                mapping.Apply(this);
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
    public MessageTypeDescriptor<T> Register<T>(string? wireName, IEnumerable<string>? aliases = null)
    {
        MessageTypeDescriptor<T> descriptor;
        if (byType.TryGetValue(typeof(T), out var existing))
        {
            descriptor = (MessageTypeDescriptor<T>)existing;
            if (wireName is not null && descriptor.WireName != wireName)
                throw new EasyNetQException(
                    "Message type {0} is already registered with wire name '{1}'; it cannot also be '{2}'",
                    typeof(T).FullName ?? typeof(T).Name, descriptor.WireName, wireName
                );
        }
        else
        {
            descriptor = (MessageTypeDescriptor<T>)Register(Populate(new MessageTypeDescriptor<T>(wireName ?? typeNameSerializer.Serialize(typeof(T)))));
            if (wireName is not null && descriptor.WireName != wireName)
                throw new EasyNetQException("Message type {0} was registered concurrently under another wire name", typeof(T).Name);
        }

        if (wireName is not null && byWireName.TryGetValue(wireName, out var wireOwner) && !ReferenceEquals(wireOwner, descriptor))
            throw new EasyNetQException(
                "Wire name '{0}' already resolves to {1}; it cannot also name {2}",
                wireName, wireOwner.DisplayName, descriptor.DisplayName
            );

        if (aliases is null) return descriptor;
        foreach (var alias in aliases)
        {
            var owner = byWireName.GetOrAdd(alias, descriptor);
            if (!ReferenceEquals(owner, descriptor))
                throw new EasyNetQException(
                    "Wire name '{0}' already resolves to {1}; it cannot alias {2}",
                    alias, owner.DisplayName, descriptor.DisplayName
                );
        }

        return descriptor;
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
    public bool TryResolveWireName(string wireName, out MessageTypeDescriptor descriptor)
    {
        if (byWireName.TryGetValue(wireName, out descriptor!))
            return true;
        try
        {
            descriptor = GetByWireName(wireName);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // malformed or foreign names fail in many ways (EasyNetQException, FileLoadException, ...)
            return false;
        }
    }

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
