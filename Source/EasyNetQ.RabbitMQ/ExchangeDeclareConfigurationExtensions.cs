using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IExchangeDeclareConfiguration"/>
/// </summary>
public static class ExchangeDeclareConfigurationExtensions
{
    /// <summary>
    /// Sets alternate exchange of the exchange.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="alternateExchange">The alternate exchange to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IExchangeDeclareConfiguration WithAlternateExchange(this IExchangeDeclareConfiguration configuration, in Exchange alternateExchange) =>
        configuration.WithArgument(Argument.AlternateExchange, alternateExchange.Name);
}
