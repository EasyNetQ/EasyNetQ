using System.Collections.Concurrent;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    /// <inheritdoc />
    public class HandlerCollectionPerQueueFactory : IHandlerCollectionFactory
    {
        private readonly ConcurrentDictionary<string, IHandlerCollection> handlerCollections = new();

        /// <inheritdoc />
        public IHandlerCollection CreateHandlerCollection(in Queue queue)
        {
            return handlerCollections.GetOrAdd(queue.Name, _ => new HandlerCollection());
        }
    }
}
