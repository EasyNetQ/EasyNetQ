using System;
using System.Collections.Concurrent;
using System.Threading;
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

        public async Task<IExchange> DeclareExchangeAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
        {
            if (exchanges.TryGetValue(exchangeName, out var exchange)) return exchange;
            using (await asyncLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                if (exchanges.TryGetValue(exchangeName, out exchange)) return exchange;
                exchange = await advancedBus.ExchangeDeclareAsync(exchangeName, exchangeType, cancellationToken: cancellationToken).ConfigureAwait(false);
                exchanges[exchangeName] = exchange;
                return exchange;
            }
        }

        public Task<IExchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken)
        {
            var exchangeName = conventions.ExchangeNamingConvention(messageType);
            return DeclareExchangeAsync(exchangeName, exchangeType, cancellationToken);
        }
    }
}