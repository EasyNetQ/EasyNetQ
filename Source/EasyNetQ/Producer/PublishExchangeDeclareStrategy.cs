using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class PublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, Task<IExchange>> exchangeNames =new ConcurrentDictionary<string, Task<IExchange>>();
     
        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            return DeclareExchangeAsync(advancedBus, exchangeName, exchangeType).Result;
        }
        
        public IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            return DeclareExchangeAsync(advancedBus, messageType, exchangeType).Result;
        }

        public Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            return exchangeNames.AddOrUpdate(
                exchangeName,
                name => advancedBus.ExchangeDeclareAsync(name, exchangeType),
                (name, exchangeTask) => exchangeTask.IsFaulted ? advancedBus.ExchangeDeclareAsync(name, exchangeType) : exchangeTask);
        }

        public Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var exchangeName = conventions.ExchangeNamingConvention(messageType);
            return DeclareExchangeAsync(advancedBus, exchangeName, exchangeType);
        }
    }
}