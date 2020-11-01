using System;
using System.Threading;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests
{
    public static class PublishExchangeDeclareStrategyExtensions
    {
        public static IExchange DeclareExchange(
            this IExchangeDeclareStrategy strategy,
            string exchangeName,
            string exchangeType,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(strategy, "strategy");

            return strategy.DeclareExchangeAsync(exchangeName, exchangeType, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        public static IExchange DeclareExchange(
            this IExchangeDeclareStrategy strategy,
            Type messageType,
            string exchangeType,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(strategy, "strategy");

            return strategy.DeclareExchangeAsync(messageType, exchangeType, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
    }
}
