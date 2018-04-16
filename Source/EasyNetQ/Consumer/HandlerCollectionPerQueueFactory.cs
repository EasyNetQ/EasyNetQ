
using System.Collections.Concurrent;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class HandlerCollectionPerQueueFactory : IHandlerCollectionFactory
    {
        readonly ConcurrentDictionary<string, IHandlerCollection> handlerCollections = new ConcurrentDictionary<string, IHandlerCollection>();

        public IHandlerCollection CreateHandlerCollection(IQueue queue)
        {
            return handlerCollections.AddOrUpdate(queue.Name,
                queueName => new HandlerCollection(),
                (queueName, existingCollection) => existingCollection);
        }
    }
}