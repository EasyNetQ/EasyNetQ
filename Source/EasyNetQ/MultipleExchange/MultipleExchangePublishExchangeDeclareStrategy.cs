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
        private readonly AsyncLock asyncLock = new AsyncLock();
        
        public IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var sourceExchangeName = conventions.ExchangeNamingConvention(messageType);
            var sourceExchange = DeclareExchange(advancedBus, sourceExchangeName, exchangeType);
            var interfaces = messageType.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                var destinationExchangeName = conventions.ExchangeNamingConvention(@interface);
                var destinationExchange = DeclareExchange(advancedBus, destinationExchangeName, exchangeType);
                if (destinationExchange != null)
                {
                    advancedBus.Bind(sourceExchange, destinationExchange, "#");
                }
            }

            return sourceExchange;
        }

        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            IExchange exchange;
            if (exchanges.TryGetValue(exchangeName, out exchange))
            {
                return exchange;
            }
            using(asyncLock.Acquire())
            {
                if (exchanges.TryGetValue(exchangeName, out exchange))
                {
                    return exchange;
                }
                exchange = advancedBus.ExchangeDeclare(exchangeName, exchangeType);
                exchanges[exchangeName] = exchange;
                return exchange;
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
                var destinationExchange = await DeclareExchangeAsync(advancedBus, destinationExchangeName, exchangeType).ConfigureAwait(false);
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
            using(await asyncLock.AcquireAsync().ConfigureAwait(false))
            {
                if (exchanges.TryGetValue(exchangeName, out exchange))
                {
                    return exchange;
                }
                exchange = await advancedBus.ExchangeDeclareAsync(exchangeName, exchangeType).ConfigureAwait(false);
                exchanges[exchangeName] = exchange;
                return exchange;
            }
        }
    }
}
