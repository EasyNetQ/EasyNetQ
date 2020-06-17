using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class HandlerCollectionFactory : IHandlerCollectionFactory
    {
        /// <inheritdoc />
        public IHandlerCollection CreateHandlerCollection(IQueue queue)
        {
            return new HandlerCollection();
        }
    }
}
