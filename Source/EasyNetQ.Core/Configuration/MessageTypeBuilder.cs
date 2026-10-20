using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Configuration;

/// <summary>
///     Configures how <typeparamref name="T" /> is named on the wire:
///     <c>MessageType&lt;OrderPlaced&gt;(m =&gt; m.WireName("orders.placed.v1").Alias("Legacy.OrderPlaced"))</c>
/// </summary>
public sealed class MessageTypeBuilder<T>
{
    internal MessageTypeBuilder(MessageTypeMapping<T> mapping) => Mapping = mapping;

    /// <summary>The mapping being built</summary>
    public MessageTypeMapping<T> Mapping { get; }

    /// <summary>
    ///     Publish and match <typeparamref name="T" /> as <paramref name="wireName" />
    /// </summary>
    public MessageTypeBuilder<T> WireName(string wireName)
    {
        Mapping.WireName = wireName;
        return this;
    }

    /// <summary>
    ///     Also resolve incoming <paramref name="wireNames" /> to <typeparamref name="T" /> (e.g. the type names
    ///     another stack sends)
    /// </summary>
    public MessageTypeBuilder<T> Alias(params string[] wireNames)
    {
        Mapping.AliasList.AddRange(wireNames);
        return this;
    }
}

/// <summary>
///     Fluent message type naming
/// </summary>
public static class EasyNetQBuilderMessageTypeExtensions
{
    /// <summary>
    ///     Configures the wire name and aliases of <typeparamref name="T" />
    /// </summary>
    public static IEasyNetQBuilder MessageType<T>(this IEasyNetQBuilder builder, Action<MessageTypeBuilder<T>> configure)
    {
        var mapping = new MessageTypeMapping<T>();
        configure(new MessageTypeBuilder<T>(mapping));
        builder.Services.AddSingleton<MessageTypeMapping>(mapping);
        return builder;
    }
}
