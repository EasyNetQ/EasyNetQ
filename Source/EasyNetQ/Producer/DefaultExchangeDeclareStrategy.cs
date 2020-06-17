using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class DefaultExchangeDeclareStrategy : IExchangeDeclareStrategy
    {
        private readonly IConventions conventions;
        private readonly AsyncCache<ExchangeKey, IExchange> declaredExchanges;

        public DefaultExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
        {
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");

            this.conventions = conventions;
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
            var exchangeName = conventions.ExchangeNamingConvention(messageType);
            return DeclareExchangeAsync(exchangeName, exchangeType, cancellationToken);
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
