using System.Collections.Concurrent;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class PublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, IExchange> exchangeNames =
            new ConcurrentDictionary<string, IExchange>();

        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName)
        {
            return exchangeNames.AddOrUpdate(
                exchangeName,
                name => advancedBus.ExchangeDeclare(name, ExchangeType.Topic),
                (_, exchange) => exchange);
        }
    }
}