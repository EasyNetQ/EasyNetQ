using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.MessageVersioning
{
    public class VersionedPublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, Task<IExchange>> exchangeNames = new ConcurrentDictionary<string, Task<IExchange>>();

        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            return DeclareExchangeAsync(advancedBus, exchangeName, exchangeType).Result;
        }

        public IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            return DeclareExchangeAsync(advancedBus, messageType, exchangeType).Result;
        }

        public Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            return exchangeNames.AddOrUpdate(
                exchangeName,
                name => advancedBus.ExchangeDeclareAsync(name, exchangeType),
                (name, exchangeTask) => exchangeTask.IsFaulted ? advancedBus.ExchangeDeclareAsync(name, exchangeType) : exchangeTask);
        }

        public Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var messageVersions = new MessageVersionStack(messageType);
            return DeclareVersionedExchanges(advancedBus, conventions, messageVersions, exchangeType);
        }

        private Task<IExchange> DeclareVersionedExchanges(IAdvancedBus advancedBus, IConventions conventions, MessageVersionStack messageVersions, string exchangeType)
        {
            var destinationExchangeTask = TaskHelpers.FromResult<IExchange>(null);
            while (! messageVersions.IsEmpty())
            {
                var messageType = messageVersions.Pop();
                var exchangeName = conventions.ExchangeNamingConvention(messageType);
                destinationExchangeTask = destinationExchangeTask.Then(destinationExchange => DeclareExchangeAsync(advancedBus, exchangeName, exchangeType).Then(sourceExchange =>
                    {
                        if (destinationExchange != null)
                            return advancedBus.BindAsync(sourceExchange, destinationExchange, "#").Then(() => sourceExchange);
                        return TaskHelpers.FromResult(sourceExchange);
                    }));
            }
            return destinationExchangeTask;
        }
    }
}