using EasyNetQ.Pipeline;

namespace EasyNetQ.Configuration;

/// <summary>
///     A publish route: the definition a message type belongs to and how its routing key is resolved
/// </summary>
public abstract class PublishRoute
{
    private protected PublishRoute(MessageTypeDescriptor descriptor, PublishDefinition definition)
    {
        Descriptor = descriptor;
        Definition = definition;
    }

    /// <summary>The message type</summary>
    public MessageTypeDescriptor Descriptor { get; }

    /// <summary>The publish definition this route belongs to</summary>
    public PublishDefinition Definition { get; }

    /// <summary>The pipeline for this route's definition, built by the publisher</summary>
    public PipelineStep<PublishContext>? Pipeline { get; set; }

    /// <summary>Resolves the routing key for a message (type-erased fallback)</summary>
    public abstract string ResolveRoutingKey(object message);
}

/// <summary>
///     A typed publish route; the routing key resolver sees the message without boxing
/// </summary>
public sealed class PublishRoute<T> : PublishRoute
{
    private readonly string? routingKey;
    private readonly Func<T, string>? routingKeyResolver;

    internal PublishRoute(MessageTypeDescriptor<T> descriptor, PublishDefinition definition, string? routingKey, Func<T, string>? routingKeyResolver)
        : base(descriptor, definition)
    {
        this.routingKey = routingKey;
        this.routingKeyResolver = routingKeyResolver;
    }

    /// <summary>Resolves the routing key for <paramref name="message" /></summary>
    public string ResolveRoutingKey(T message) => routingKeyResolver?.Invoke(message) ?? routingKey ?? "";

    /// <inheritdoc />
    public override string ResolveRoutingKey(object message) => ResolveRoutingKey((T)message);
}

/// <summary>
///     Maps message types to publish routes. A type publishes through exactly one route; registering it in two
///     definitions is a configuration error.
/// </summary>
public sealed class PublishRouteTable
{
    private readonly IMessageTypeRegistry registry;
    private readonly Dictionary<Type, PublishRoute> routes = new();

    /// <summary>
    ///     Creates the table over <paramref name="registry" />
    /// </summary>
    public PublishRouteTable(IMessageTypeRegistry registry) => this.registry = registry;

    /// <summary>All routes by message type</summary>
    public IReadOnlyDictionary<Type, PublishRoute> Routes => routes;

    /// <summary>
    ///     Routes <typeparamref name="T" /> through <paramref name="definition" /> with a fixed routing key
    /// </summary>
    public PublishRouteTable Add<T>(PublishDefinition definition, string? routingKey)
        => Add(typeof(T), new PublishRoute<T>(registry.GetOrAdd<T>(), definition, routingKey, null));

    /// <summary>
    ///     Routes <typeparamref name="T" /> through <paramref name="definition" /> with a per-message routing key
    /// </summary>
    public PublishRouteTable Add<T>(PublishDefinition definition, Func<T, string> routingKeyResolver)
        => Add(typeof(T), new PublishRoute<T>(registry.GetOrAdd<T>(), definition, null, routingKeyResolver));

    private PublishRouteTable Add(Type type, PublishRoute route)
    {
        if (!routes.TryAdd(type, route))
            throw new InvalidOperationException($"Message type {type} is registered in more than one publish definition");
        return this;
    }
}
