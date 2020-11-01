using System.Collections.Concurrent;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class HandlerCollectionPerQueueFactory : IHandlerCollectionFactory
    {
        private readonly ConcurrentDictionary<string, IHandlerCollection> handlerCollections = new ConcurrentDictionary<string, IHandlerCollection>();

        /// <inheritdoc />
        public IHandlerCollection CreateHandlerCollection(IQueue queue)
        {
            return handlerCollections.GetOrAdd(queue.Name, _ => new HandlerCollection());
        }
    }
}
