using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.MessageVersioning
{
    public class VersionedPublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, IExchange> exchanges = new ConcurrentDictionary<string, IExchange>();
        private readonly AsyncSemaphore semaphore = new AsyncSemaphore(1);

        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            IExchange exchange;
            if (exchanges.TryGetValue(exchangeName, out exchange))
            {
                return exchange;
            }
            semaphore.Wait();
            try
            {
                if (exchanges.TryGetValue(exchangeName, out exchange))
                {
                    return exchange;
                }
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
            var messageVersions = new MessageVersionStack(messageType);
            return DeclareVersionedExchanges(advancedBus, conventions, messageVersions, exchangeType);
        }

        public async Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            IExchange exchange;
            if (exchanges.TryGetValue(exchangeName, out exchange))
            {
                return exchange;
            }
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (exchanges.TryGetValue(exchangeName, out exchange))
                {
                    return exchange;
                }
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
            var messageVersions = new MessageVersionStack(messageType);
            return DeclareVersionedExchangesAsync(advancedBus, conventions, messageVersions, exchangeType);
        }

        private async Task<IExchange> DeclareVersionedExchangesAsync(IAdvancedBus advancedBus, IConventions conventions, MessageVersionStack messageVersions, string exchangeType)
        {
            IExchange destinationExchange = null;
            while (! messageVersions.IsEmpty())
            {
                var messageType = messageVersions.Pop();
                var exchangeName = conventions.ExchangeNamingConvention(messageType);
                var sourceExchange = await DeclareExchangeAsync(advancedBus, exchangeName, exchangeType).ConfigureAwait(false);
                if (destinationExchange != null)
                {
                    await advancedBus.BindAsync(sourceExchange, destinationExchange, "#").ConfigureAwait(false);
                }
                destinationExchange = sourceExchange;
            }
            return destinationExchange;
        }

        private IExchange DeclareVersionedExchanges(IAdvancedBus advancedBus, IConventions conventions, MessageVersionStack messageVersions, string exchangeType)
        {
            IExchange destinationExchange = null;
            while (!messageVersions.IsEmpty())
            {
                var messageType = messageVersions.Pop();
                var exchangeName = conventions.ExchangeNamingConvention(messageType);
                var sourceExchange = DeclareExchange(advancedBus, exchangeName, exchangeType);
                if (destinationExchange != null)
                {
                    advancedBus.Bind(sourceExchange, destinationExchange, "#");
                }
                destinationExchange = sourceExchange;
            }
            return destinationExchange;
        }
    }
}