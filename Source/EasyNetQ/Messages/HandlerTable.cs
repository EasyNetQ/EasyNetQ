using System.Collections.Concurrent;
using EasyNetQ.Pipeline;

namespace EasyNetQ;

/// <summary>
///     Represents a delegate which handles a deserialized message body. This is the reflection-free replacement for
///     <see cref="Consumer.IMessageHandler{T}" />: no <see cref="IMessage" /> envelope is allocated.
/// </summary>
public delegate ValueTask<AckDecision> MessageHandler<in T>(T body, ConsumeContext context);

/// <summary>
///     A handler registered in a <see cref="HandlerTable" />, closed over its message type via the generic subclass
/// </summary>
public abstract class HandlerEntry
{
    private protected HandlerEntry(MessageTypeDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    /// <summary>
    ///     The message type this handler consumes
    /// </summary>
    public MessageTypeDescriptor Descriptor { get; }

    /// <summary>
    ///     Invokes the handler with the already-deserialized <see cref="ConsumeContext.Message" />
    /// </summary>
    public abstract ValueTask<AckDecision> InvokeAsync(ConsumeContext context);
}

internal sealed class HandlerEntry<T> : HandlerEntry
{
    private readonly MessageHandler<T> handler;

    public HandlerEntry(MessageTypeDescriptor<T> descriptor, MessageHandler<T> handler) : base(descriptor)
    {
        this.handler = handler;
    }

    public override ValueTask<AckDecision> InvokeAsync(ConsumeContext context) => handler((T)context.Message!, context);
}

/// <summary>
///     The handlers of one consumer, keyed by wire type name. Lookup is a dictionary hit for exact wire names;
///     unknown names are resolved through the registry once, matched exactly or polymorphically against the
///     registered handlers, and the resolution is cached.
/// </summary>
public sealed class HandlerTable
{
    private static readonly HandlerEntry NoopEntry = new NoopHandlerEntry();

    private readonly IMessageTypeRegistry registry;
    // exact registrations only: an entry's own wire name -> entry (safe for descriptor resolution)
    private readonly ConcurrentDictionary<string, HandlerEntry> registrationsByWireName = new();
    private readonly ConcurrentDictionary<Type, HandlerEntry> byType = new();
    // handler-resolution cache, including polymorphic matches; NEVER used to resolve descriptors, because a
    // derived message's wire name may map to a base type's handler here
    private readonly ConcurrentDictionary<string, HandlerEntry> resolvedByWireName = new();

    /// <summary>
    ///     Creates a handler table
    /// </summary>
    public HandlerTable(IMessageTypeRegistry registry)
    {
        this.registry = registry;
    }

    /// <summary>
    ///     Set to false to silently acknowledge messages no handler matches instead of failing them
    /// </summary>
    public bool ThrowOnNoMatchingHandler { get; set; } = true;

    /// <summary>
    ///     Registered handlers
    /// </summary>
    public IReadOnlyCollection<HandlerEntry> Entries => (IReadOnlyCollection<HandlerEntry>)byType.Values;

    /// <summary>
    ///     Registers a handler for <typeparamref name="T" />
    /// </summary>
    public HandlerTable Add<T>(MessageHandler<T> handler)
    {
        var descriptor = registry.GetOrAdd<T>();
        var entry = new HandlerEntry<T>(descriptor, handler);
        if (!byType.TryAdd(typeof(T), entry))
            throw new EasyNetQException("There is already a handler for message type '{0}'", typeof(T).Name);
        registrationsByWireName[descriptor.WireName] = entry;
        resolvedByWireName[descriptor.WireName] = entry;
        return this;
    }

    /// <summary>
    ///     Resolves the descriptor for an incoming wire type name: a registered handler's descriptor when the name
    ///     matches exactly, otherwise the registry's resolution of the name
    /// </summary>
    public MessageTypeDescriptor ResolveDescriptor(string? wireName)
    {
        if (wireName is not null && registrationsByWireName.TryGetValue(wireName, out var entry))
            return entry.Descriptor;
        if (wireName is null)
            throw new EasyNetQException("Received message has no type property; a typed consumer cannot dispatch it");
        return registry.GetByWireName(wireName);
    }

    /// <summary>
    ///     Resolves the handler for a message type: exact match first, then a handler of an assignable type
    ///     (cached after the first scan)
    /// </summary>
    public HandlerEntry Resolve(MessageTypeDescriptor descriptor)
    {
        if (resolvedByWireName.TryGetValue(descriptor.WireName, out var entry))
            return entry;

        if (byType.TryGetValue(descriptor.Type, out entry))
        {
            resolvedByWireName.TryAdd(descriptor.WireName, entry);
            return entry;
        }

        // polymorphic fallback: a handler for a base type or interface of the consumed message
        foreach (var kvp in byType)
        {
            if (!kvp.Key.IsAssignableFrom(descriptor.Type)) continue;
            resolvedByWireName.TryAdd(descriptor.WireName, kvp.Value);
            return kvp.Value;
        }

        if (ThrowOnNoMatchingHandler)
            throw new EasyNetQException("No handler found for message type {0}", descriptor.Type.Name);

        return NoopEntry;
    }

    private sealed class NoopHandlerEntry : HandlerEntry
    {
        public NoopHandlerEntry() : base(new MessageTypeDescriptor<object>("noop"))
        {
        }

        public override ValueTask<AckDecision> InvokeAsync(ConsumeContext context) => new(AckDecision.Ack);
    }
}
