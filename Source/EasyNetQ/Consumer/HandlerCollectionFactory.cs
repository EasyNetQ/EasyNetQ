using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class HandlerCollectionFactory : IHandlerCollectionFactory
    {
        /// <inheritdoc />
        public IHandlerCollection CreateHandlerCollection(in Queue queue)
        {
            return new HandlerCollection();
        }
    }
}
