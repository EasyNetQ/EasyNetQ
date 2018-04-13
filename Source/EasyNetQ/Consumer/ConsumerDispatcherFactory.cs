using System;

namespace EasyNetQ.Consumer
{
    /// <summary>
    /// The default ConsumerDispatcherFactory. It creates a single dispatch
    /// queue which all consumers share.
    /// </summary>
    public class ConsumerDispatcherFactory : IConsumerDispatcherFactory
    {
        private readonly Lazy<IConsumerDispatcher> dispatcher;

        public ConsumerDispatcherFactory(ConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            
            dispatcher = new Lazy<IConsumerDispatcher>(() => new ConsumerDispatcher(configuration));
        }

        public IConsumerDispatcher GetConsumerDispatcher()
        {
            return dispatcher.Value;
        }

        public void OnDisconnected()
        {
            if (dispatcher.IsValueCreated)
            {
                dispatcher.Value.OnDisconnected();
            }
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