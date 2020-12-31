using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.MessageVersioning
{
    /// <inheritdoc />
    public class VersionedExchangeDeclareStrategy : IExchangeDeclareStrategy
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly AsyncCache<ExchangeKey, Exchange> declaredExchanges;

        public VersionedExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
        {
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");

            this.conventions = conventions;
            this.advancedBus = advancedBus;

            declaredExchanges = new AsyncCache<ExchangeKey, Exchange>((k, c) => advancedBus.ExchangeDeclareAsync(k.Name, k.Type, cancellationToken: c));
        }

        /// <inheritdoc />
        public Task<Exchange> DeclareExchangeAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
        {
            return declaredExchanges.GetOrAddAsync(new ExchangeKey(exchangeName, exchangeType), cancellationToken);
        }

        /// <inheritdoc />
        public Task<Exchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken)
        {
            var messageVersions = new MessageVersionStack(messageType);
            return DeclareVersionedExchangesAsync(messageVersions, exchangeType, cancellationToken);
        }

        private async Task<Exchange> DeclareVersionedExchangesAsync(MessageVersionStack messageVersions, string exchangeType, CancellationToken cancellationToken)
        {
            Exchange? destinationExchange = null;
            while (!messageVersions.IsEmpty())
            {
                var messageType = messageVersions.Pop();
                var exchangeName = conventions.ExchangeNamingConvention(messageType);
                var sourceExchange = await DeclareExchangeAsync(exchangeName, exchangeType, cancellationToken).ConfigureAwait(false);
                if (destinationExchange != null)
                    await advancedBus.BindAsync(sourceExchange, destinationExchange.Value, "#", cancellationToken).ConfigureAwait(false);
                destinationExchange = sourceExchange;
            }
            return destinationExchange ?? throw new ArgumentOutOfRangeException(nameof(messageVersions));
        }

        private readonly struct ExchangeKey
        {
            public ExchangeKey(string name, string type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }

            public string Type { get; }
        }
    }
}
