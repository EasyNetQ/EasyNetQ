using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class HandlerCollectionFactory : IHandlerCollectionFactory
    {
        public IHandlerCollection CreateHandlerCollection(IQueue queue)
        {
            return new HandlerCollection();
        }
    }
}