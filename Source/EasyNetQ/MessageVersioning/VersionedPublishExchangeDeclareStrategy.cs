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
        private readonly IAdvancedBus advancedBus;
        private readonly AsyncLock asyncLock = new AsyncLock();
        private readonly IConventions conventions;
        private readonly ConcurrentDictionary<string, IExchange> exchanges = new ConcurrentDictionary<string, IExchange>();

        public VersionedPublishExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
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
            var messageVersions = new MessageVersionStack(messageType);
            return DeclareVersionedExchanges(messageVersions, exchangeType);
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
            var messageVersions = new MessageVersionStack(messageType);
            return DeclareVersionedExchangesAsync(messageVersions, exchangeType);
        }

        private async Task<IExchange> DeclareVersionedExchangesAsync(MessageVersionStack messageVersions, string exchangeType)
        {
            IExchange destinationExchange = null;
            while (!messageVersions.IsEmpty())
            {
                var messageType = messageVersions.Pop();
                var exchangeName = conventions.ExchangeNamingConvention(messageType);
                var sourceExchange = await DeclareExchangeAsync(exchangeName, exchangeType).ConfigureAwait(false);
                if (destinationExchange != null)
                    await advancedBus.BindAsync(sourceExchange, destinationExchange, "#").ConfigureAwait(false);
                destinationExchange = sourceExchange;
            }

            return destinationExchange;
        }

        private IExchange DeclareVersionedExchanges(MessageVersionStack messageVersions, string exchangeType)
        {
            IExchange destinationExchange = null;
            while (!messageVersions.IsEmpty())
            {
                var messageType = messageVersions.Pop();
                var exchangeName = conventions.ExchangeNamingConvention(messageType);
                var sourceExchange = DeclareExchange(exchangeName, exchangeType);
                if (destinationExchange != null) advancedBus.Bind(sourceExchange, destinationExchange, "#");
                destinationExchange = sourceExchange;
            }

            return destinationExchange;
        }
    }
}