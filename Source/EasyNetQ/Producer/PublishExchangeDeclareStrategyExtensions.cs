using System;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public static class PublishExchangeDeclareStrategyExtensions
    {
        public static IExchange DeclareExchange(
            this IPublishExchangeDeclareStrategy strategy, 
            IAdvancedBus advancedBus,
            string exchangeName,
            string exchangeType)
        {
            Preconditions.CheckNotNull(strategy, "strategy");
            
            return strategy.DeclareExchangeAsync(advancedBus, exchangeName, exchangeType)
                .GetAwaiter()
                .GetResult();
        }
        
        public static IExchange DeclareExchange(
            this IPublishExchangeDeclareStrategy strategy, 
            IAdvancedBus advancedBus,
            Type messageType,
            string exchangeType)
        {
            Preconditions.CheckNotNull(strategy, "strategy");
            
            return strategy.DeclareExchangeAsync(advancedBus, messageType, exchangeType)
                .GetAwaiter()
                .GetResult();
        }
    }
}