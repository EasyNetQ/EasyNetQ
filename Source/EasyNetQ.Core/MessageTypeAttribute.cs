namespace EasyNetQ;

/// <summary>
///     Fixes the wire type name (the AMQP "type" property) of a message type, and optionally the extra incoming
///     names it answers to. Read by the source generator, so it costs no runtime reflection and works under
///     Native AOT. Use it for contracts shared with other stacks, where the name is part of the contract.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
public sealed class MessageTypeAttribute : Attribute
{
    /// <summary>
    ///     Creates the attribute
    /// </summary>
    public MessageTypeAttribute(string wireName) => WireName = wireName;

    /// <summary>The wire type name to publish and match</summary>
    public string WireName { get; }

    /// <summary>Additional incoming wire names that resolve to this type</summary>
    public string[]? Aliases { get; init; }
}
