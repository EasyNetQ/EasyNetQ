namespace EasyNetQ.Consumer
{
    public class HandlerCollectionFactory : IHandlerCollectionFactory
    {
        private readonly IEasyNetQLogger logger;

        public HandlerCollectionFactory(IEasyNetQLogger logger)
        {
            this.logger = logger;
        }

        public IHandlerCollection CreateHandlerCollection()
        {
            return new HandlerCollection(logger);
        }
    }
}