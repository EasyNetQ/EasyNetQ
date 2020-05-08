using System;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public static class DelayedExchangeExtensions
    {
        public static IExchangeDeclareConfiguration AsDelayedExchange(this IExchangeDeclareConfiguration configuration, string exchangeType = ExchangeType.Fanout)
        {
            return configuration.WithType("x-delayed-message")
                .WithArgument("x-delayed-type", exchangeType);
        }

        public static IMessage<T> WithDelay<T>(this IMessage<T> message, TimeSpan delay)
        {
            Preconditions.CheckNotNull(message, "message");

            message.Properties.Headers["x-delay"] = (int)delay.TotalMilliseconds;
            return message;
        }
    }
}
