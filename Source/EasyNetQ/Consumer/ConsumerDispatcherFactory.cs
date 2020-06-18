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

        /// <summary>
        ///     Creates ConsumerDispatcher
        /// </summary>
        public ConsumerDispatcherFactory()
        {
            dispatcher = new Lazy<IConsumerDispatcher>(() => new ConsumerDispatcher());
        }

        /// <inheritdoc />
        public IConsumerDispatcher GetConsumerDispatcher()
        {
            return dispatcher.Value;
        }

        /// <inheritdoc />
        public void OnDisconnected()
        {
            if (dispatcher.IsValueCreated)
            {
                dispatcher.Value.OnDisconnected();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (dispatcher.IsValueCreated)
            {
                dispatcher.Value.Dispose();
            }
        }
    }
}
