
using System.Collections.Concurrent;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class HandlerCollectionPerQueueFactory : IHandlerCollectionFactory
    {
        readonly ConcurrentDictionary<string, IHandlerCollection> handlerCollections = new ConcurrentDictionary<string, IHandlerCollection>();
        readonly IEasyNetQLogger logger;

        public HandlerCollectionPerQueueFactory(IEasyNetQLogger logger)
        {
            this.logger = logger;
        }

        public IHandlerCollection CreateHandlerCollection(IQueue queue)
        {
            return handlerCollections.AddOrUpdate(queue.Name,
                queueName => new HandlerCollection(logger),
                (queueName, existingCollection) => existingCollection);
        }
    }
}