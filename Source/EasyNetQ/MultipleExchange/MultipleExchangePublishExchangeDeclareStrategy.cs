using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;


namespace EasyNetQ.MultipleExchange
{
    public class MultipleExchangePublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, IExchange> exchanges = new ConcurrentDictionary<string, IExchange>();
        private readonly AsyncSemaphore semaphore = new AsyncSemaphore(1);
        public IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            return DeclareExchangeAsync(advancedBus, messageType, exchangeType).Result;
        }

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

        public async Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var sourceExchangeName = conventions.ExchangeNamingConvention(messageType);
            var sourceExchange = await DeclareExchangeAsync(advancedBus, sourceExchangeName, exchangeType).ConfigureAwait(false);
            var interfaces = messageType.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                var destinationExchangeName = conventions.ExchangeNamingConvention(@interface);
                var destinationExchange = await DeclareExchangeAsync(advancedBus, destinationExchangeName, exchangeType);
                if (destinationExchange != null)
                {
                    await advancedBus.BindAsync(sourceExchange, destinationExchange, "#").ConfigureAwait(false);
                }
            }

            return sourceExchange;
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
    }
}
