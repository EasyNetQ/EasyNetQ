using System;
using System.Threading;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public static class PublishExchangeDeclareStrategyExtensions
    {
        public static IExchange DeclareExchange(
            this IPublishExchangeDeclareStrategy strategy, 
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
            this IPublishExchangeDeclareStrategy strategy, 
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