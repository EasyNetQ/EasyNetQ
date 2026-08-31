namespace EasyNetQ;

/// <summary>
/// Allows consumer configuration to be fluently extended without adding overloads to IBus
///
/// e.g.
/// x => x.WithPrefetchCount(42)
/// </summary>
public interface ISimpleConsumeConfiguration
{
    /// <summary>
    ///     Automatically acknowledge a message
    /// </summary>
    /// <returns>ISimpleConsumeConfiguration</returns>
    ISimpleConsumeConfiguration WithAutoAck();

    /// <summary>
    /// Sets consumer tag
    /// </summary>
    /// <param name="consumerTag">The consumerTag to set</param>
    /// <returns>ISimpleConsumeConfiguration</returns>
    ISimpleConsumeConfiguration WithConsumerTag(string consumerTag);

    /// <summary>
    ///     Switch a consumer to exclusive mode
    /// </summary>
    /// <returns>ISimpleConsumeConfiguration</returns>
    ISimpleConsumeConfiguration WithExclusive(bool isExclusive = true);

    /// <summary>
    /// Sets a raw argument for consumer declaration
    /// </summary>
    /// <param name="name">The argument name to set</param>
    /// <param name="value">The argument value to set</param>
    /// <returns>ISimpleConsumeConfiguration</returns>
    ISimpleConsumeConfiguration WithArgument(string name, object value);

    /// <summary>
    ///     Sets prefetch count
    /// </summary>
    /// <param name="prefetchCount">The prefetchCount to set</param>
    /// <returns>ISimpleConsumeConfiguration</returns>
    ISimpleConsumeConfiguration WithPrefetchCount(ushort prefetchCount);
}

/// <summary>
///     Various extensions for <see cref="ISimpleConsumeConfiguration"/>
/// </summary>
public static class SimpleConsumeConfigurationExtensions
{
    /// <summary>
    ///     Sets priority
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="priority">The priority to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static ISimpleConsumeConfiguration WithPriority(this ISimpleConsumeConfiguration configuration, int priority)
        => configuration.WithArgument("x-priority", priority);
}
