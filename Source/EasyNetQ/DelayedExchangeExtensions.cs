using System;
using EasyNetQ.Topology;

namespace EasyNetQ
{
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
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithType("x-delayed-message")
                .WithArgument("x-delayed-type", exchangeType);
        }

        /// <summary>
        ///     Add the delay to the message to be used by delayed exchange
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="delay">The delay</param>
        public static IMessage<T> WithDelay<T>(this IMessage<T> message, TimeSpan delay)
        {
            Preconditions.CheckNotNull(message, "message");

            message.Properties.Headers["x-delay"] = (int)delay.TotalMilliseconds;
            return message;
        }
    }
}
