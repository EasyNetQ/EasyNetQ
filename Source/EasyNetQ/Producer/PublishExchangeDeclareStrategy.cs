using System;
using System.Collections.Concurrent;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class PublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, IExchange> exchangeNames =
            new ConcurrentDictionary<string, IExchange>();
     
        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            return exchangeNames.AddOrUpdate(
                exchangeName,
                name => advancedBus.ExchangeDeclare(name, exchangeType),
                (_, exchange) => exchange);
        }
        
        public IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var exchangeName = conventions.ExchangeNamingConvention(messageType);
            return DeclareExchange(advancedBus, exchangeName, exchangeType);
        }        
    }
}