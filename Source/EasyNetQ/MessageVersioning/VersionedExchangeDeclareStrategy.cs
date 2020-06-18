using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.MessageVersioning
{
    public class VersionedExchangeDeclareStrategy : IExchangeDeclareStrategy
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly AsyncCache<ExchangeKey, IExchange> declaredExchanges;

        public VersionedExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
        {
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");

            this.conventions = conventions;
            this.advancedBus = advancedBus;

            declaredExchanges = new AsyncCache<ExchangeKey, IExchange>((k, c) => advancedBus.ExchangeDeclareAsync(k.Name, k.Type, cancellationToken: c));
        }

        /// <inheritdoc />
        public Task<IExchange> DeclareExchangeAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
        {
            return declaredExchanges.GetOrAddAsync(new ExchangeKey(exchangeName, exchangeType), cancellationToken);
        }

        /// <inheritdoc />
        public Task<IExchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken)
        {
            var messageVersions = new MessageVersionStack(messageType);
            return DeclareVersionedExchangesAsync(messageVersions, exchangeType, cancellationToken);
        }

        private async Task<IExchange> DeclareVersionedExchangesAsync(MessageVersionStack messageVersions, string exchangeType, CancellationToken cancellationToken)
        {
            IExchange destinationExchange = null;
            while (!messageVersions.IsEmpty())
            {
                var messageType = messageVersions.Pop();
                var exchangeName = conventions.ExchangeNamingConvention(messageType);
                var sourceExchange = await DeclareExchangeAsync(exchangeName, exchangeType, cancellationToken).ConfigureAwait(false);
                if (destinationExchange != null)
                    await advancedBus.BindAsync(sourceExchange, destinationExchange, "#", cancellationToken).ConfigureAwait(false);
                destinationExchange = sourceExchange;
            }

            return destinationExchange;
        }

        private struct ExchangeKey
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
