namespace EasyNetQ;

/// <summary>
///     The wire name and aliases configured for one message type (<c>MessageType&lt;T&gt;(...)</c> or
///     <see cref="MessageTypeAttribute" />). The registry applies mappings before the generated initializers.
/// </summary>
public abstract class MessageTypeMapping
{
    private protected MessageTypeMapping()
    {
    }

    /// <summary>The message type</summary>
    public abstract Type MessageType { get; }

    /// <summary>The wire type name to publish and match; null keeps the type name serializer's name</summary>
    public string? WireName { get; internal set; }

    /// <summary>Additional incoming wire names that resolve to <see cref="MessageType" /></summary>
    public IReadOnlyList<string> Aliases => AliasList;

    internal List<string> AliasList { get; } = new();

    internal abstract void Apply(IMessageTypeRegistry registry);
}

/// <summary>
///     The typed mapping: applying it is a closed generic <c>Register&lt;T&gt;</c> call, so it stays reflection-free
/// </summary>
public sealed class MessageTypeMapping<T> : MessageTypeMapping
{
    /// <inheritdoc />
    public override Type MessageType => typeof(T);

    internal override void Apply(IMessageTypeRegistry registry) => registry.Register<T>(WireName, AliasList);
}
