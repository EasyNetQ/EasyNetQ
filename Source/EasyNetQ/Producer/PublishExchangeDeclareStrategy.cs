using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class PublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly IConventions _conventions;
        private readonly IAdvancedPublishExchangeDeclareStrategy _advancedPublishExchangeDeclareStrategy;

        public PublishExchangeDeclareStrategy(IConventions conventions, IAdvancedPublishExchangeDeclareStrategy advancedPublishExchangeDeclareStrategy)
        {
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedPublishExchangeDeclareStrategy, "advancedPublishExchangeDeclareStrategy");
            _conventions = conventions;
            _advancedPublishExchangeDeclareStrategy = advancedPublishExchangeDeclareStrategy;
        }

        public IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            return DeclareExchangeAsync(advancedBus, messageType, exchangeType).Result;
        }

        public Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var exchangeName = _conventions.ExchangeNamingConvention(messageType);
            return _advancedPublishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, exchangeName, exchangeType);
        }
    }

    public class AdvancedPublishExchangeDeclareStrategy : IAdvancedPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, Task<IExchange>> _exchangeNames = new ConcurrentDictionary<string, Task<IExchange>>();

        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            return DeclareExchangeAsync(advancedBus, exchangeName, exchangeType).Result;
        }

        public Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            return _exchangeNames.AddOrUpdate(
                exchangeName,
                name => advancedBus.ExchangeDeclareAsync(name, exchangeType),
                (_, exchangeTask) => exchangeTask);
        }
    }


}