using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.MultipleExchange
{
    public class MultipleExchangeDeclareStrategy : IExchangeDeclareStrategy
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly AsyncCache<ExchangeKey, IExchange> declaredExchanges;

        public MultipleExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
        {
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");

            this.conventions = conventions;
            this.advancedBus = advancedBus;

            declaredExchanges = new AsyncCache<ExchangeKey, IExchange>((k, c) => advancedBus.ExchangeDeclareAsync(k.Name, k.Type, cancellationToken: c));
        }

        /// <inheritdoc />
        public async Task<IExchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken)
        {
            var sourceExchangeName = conventions.ExchangeNamingConvention(messageType);
            var sourceExchange = await DeclareExchangeAsync(sourceExchangeName, exchangeType, cancellationToken).ConfigureAwait(false);
            var interfaces = messageType.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                var destinationExchangeName = conventions.ExchangeNamingConvention(@interface);
                var destinationExchange =
                    await DeclareExchangeAsync(destinationExchangeName, exchangeType, cancellationToken).ConfigureAwait(false);
                if (destinationExchange != null)
                    await advancedBus.BindAsync(sourceExchange, destinationExchange, "#", cancellationToken).ConfigureAwait(false);
            }

            return sourceExchange;
        }

        /// <inheritdoc />
        public Task<IExchange> DeclareExchangeAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
        {
            return declaredExchanges.GetOrAddAsync(new ExchangeKey(exchangeName, exchangeType), cancellationToken);
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
