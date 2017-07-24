using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class HandlerCollectionFactory : IHandlerCollectionFactory
    {
        private readonly IEasyNetQLogger logger;

        public HandlerCollectionFactory(IEasyNetQLogger logger)
        {
            this.logger = logger;
        }

        public IHandlerCollection CreateHandlerCollection(IQueue queue)
        {
            return new HandlerCollection(logger);
        }
    }
}