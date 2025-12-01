namespace EasyNetQ;

/// <summary>
///     Extensions related to using delayed exchange
/// </summary>
public static class DelayedExchangeExtensions
{
    /// <summary>
    ///     Marks an exchange as delayed
    /// </summary>
    /// <param name="configuration">The configuration</param>
    /// <param name="exchangeType">The exchange type</param>
    public static IExchangeDeclareConfiguration AsDelayedExchange(
        this IExchangeDeclareConfiguration configuration, string exchangeType = ExchangeType.Fanout
    ) => configuration.WithType("x-delayed-message").WithArgument("x-delayed-type", exchangeType);

    /// <summary>
    ///     Add the delay to the message properties to be used by delayed exchange
    /// </summary>
    /// <param name="messageProperties">The message properties</param>
    /// <param name="delay">The delay</param>
    public static MessageProperties WithDelay(in this MessageProperties messageProperties, TimeSpan delay)
        => messageProperties.SetHeader("x-delay", (int)delay.TotalMilliseconds);
}
