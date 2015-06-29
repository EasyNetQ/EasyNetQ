using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class PublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, IExchange> exchanges = new ConcurrentDictionary<string, IExchange>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            IExchange exchange;
            if(exchanges.TryGetValue(exchangeName, out exchange))
                return exchange;
            semaphore.Wait();
            try
            {
                if (exchanges.TryGetValue(exchangeName, out exchange))
                    return exchange;
                exchange = advancedBus.ExchangeDeclare(exchangeName, exchangeType);
                exchanges[exchangeName] = exchange;
                return exchange;
            }
            finally
            {
                semaphore.Release();
            }
        }
        
        public IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var exchangeName = conventions.ExchangeNamingConvention(messageType);
            return DeclareExchange(advancedBus, exchangeName, exchangeType);
        }

        public async Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            IExchange exchange;
            if (exchanges.TryGetValue(exchangeName, out exchange))
                return exchange;
            // TODO Need async waiting here
            // http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266983.aspx
            semaphore.Wait();
            try
            {
                if (exchanges.TryGetValue(exchangeName, out exchange))
                    return exchange;
                exchange = await advancedBus.ExchangeDeclareAsync(exchangeName, exchangeType).ConfigureAwait(false);
                exchanges[exchangeName] = exchange;
                return exchange;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var exchangeName = conventions.ExchangeNamingConvention(messageType);
            return DeclareExchangeAsync(advancedBus, exchangeName, exchangeType);
        }
    }
}