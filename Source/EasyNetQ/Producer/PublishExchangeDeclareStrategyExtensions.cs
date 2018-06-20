using System;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public static class PublishExchangeDeclareStrategyExtensions
    {
        public static IExchange DeclareExchange(
            this IPublishExchangeDeclareStrategy strategy, 
            string exchangeName,
            string exchangeType)
        {
            Preconditions.CheckNotNull(strategy, "strategy");

            return strategy.DeclareExchangeAsync(exchangeName, exchangeType)
                .GetAwaiter()
                .GetResult();
        }
        
        public static IExchange DeclareExchange(
            this IPublishExchangeDeclareStrategy strategy, 
            Type messageType,
            string exchangeType)
        {
            Preconditions.CheckNotNull(strategy, "strategy");

            return strategy.DeclareExchangeAsync(messageType, exchangeType)
                .GetAwaiter()
                .GetResult();
        }
    }
}