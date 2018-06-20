using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class PublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly IAdvancedBus advancedBus;
        private readonly AsyncLock asyncLock = new AsyncLock();
        private readonly IConventions conventions;
        private readonly ConcurrentDictionary<string, IExchange> exchanges = new ConcurrentDictionary<string, IExchange>();

        public PublishExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
        {
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");

            this.conventions = conventions;
            this.advancedBus = advancedBus;
        }

        public IExchange DeclareExchange(string exchangeName, string exchangeType)
        {
            if (exchanges.TryGetValue(exchangeName, out var exchange)) return exchange;
            using (asyncLock.Acquire())
            {
                if (exchanges.TryGetValue(exchangeName, out exchange)) return exchange;
                exchange = advancedBus.ExchangeDeclare(exchangeName, exchangeType);
                exchanges[exchangeName] = exchange;
                return exchange;
            }
        }

        public IExchange DeclareExchange(Type messageType, string exchangeType)
        {
            var exchangeName = conventions.ExchangeNamingConvention(messageType);
            return DeclareExchange(exchangeName, exchangeType);
        }

        public async Task<IExchange> DeclareExchangeAsync(string exchangeName, string exchangeType)
        {
            if (exchanges.TryGetValue(exchangeName, out var exchange)) return exchange;
            using (await asyncLock.AcquireAsync().ConfigureAwait(false))
            {
                if (exchanges.TryGetValue(exchangeName, out exchange)) return exchange;
                exchange = await advancedBus.ExchangeDeclareAsync(exchangeName, exchangeType).ConfigureAwait(false);
                exchanges[exchangeName] = exchange;
                return exchange;
            }
        }

        public Task<IExchange> DeclareExchangeAsync(Type messageType, string exchangeType)
        {
            var exchangeName = conventions.ExchangeNamingConvention(messageType);
            return DeclareExchangeAsync(exchangeName, exchangeType);
        }
    }
}