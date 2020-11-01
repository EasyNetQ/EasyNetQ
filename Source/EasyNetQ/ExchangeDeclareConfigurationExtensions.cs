using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for IExchangeDeclareConfiguration
    /// </summary>
    public static class ExchangeDeclareConfigurationExtensions
    {
        /// <summary>
        /// Sets alternate exchange of the exchange.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="alternateExchange">The alternate exchange to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IExchangeDeclareConfiguration WithAlternateExchange(
            this IExchangeDeclareConfiguration configuration, IExchange alternateExchange
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("alternate-exchange", alternateExchange.Name);
        }
    }
}
