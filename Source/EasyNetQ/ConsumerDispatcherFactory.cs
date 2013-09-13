using System;

namespace EasyNetQ
{
    /// <summary>
    /// The default ConsumerDispatcherFactory. It creates a single dispatch
    /// queue which all consumers share.
    /// </summary>
    public class ConsumerDispatcherFactory : IConsumerDispatcherFactory
    {
        private readonly Lazy<IConsumerDispatcher> dispatcher;

        public ConsumerDispatcherFactory(IEasyNetQLogger logger)
        {
            dispatcher = new Lazy<IConsumerDispatcher>(() => new ConsumerDispatcher(logger));
        }

        public IConsumerDispatcher GetConsumerDispatcher()
        {
            return dispatcher.Value;
        }

        public void Dispose()
        {
            if (dispatcher.IsValueCreated)
            {
                dispatcher.Value.Dispose();
            }
        }
    }
}