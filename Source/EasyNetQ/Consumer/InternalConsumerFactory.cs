using System;

namespace EasyNetQ.Consumer
{
    public interface IInternalConsumerFactory : IDisposable
    {
        IInternalConsumer CreateConsumer();
        void OnDisconnected();
    }

    /// <inheritdoc />
    public class InternalConsumerFactory : IInternalConsumerFactory
    {
        private readonly IPersistentConnection connection;
        private readonly IConsumerDispatcherFactory consumerDispatcherFactory;
        private readonly IConventions conventions;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;

        public InternalConsumerFactory(
            IPersistentConnection connection,
            IHandlerRunner handlerRunner,
            IConventions conventions,
            IConsumerDispatcherFactory consumerDispatcherFactory,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(consumerDispatcherFactory, "consumerDispatcherFactory");

            this.connection = connection;
            this.handlerRunner = handlerRunner;
            this.conventions = conventions;
            this.consumerDispatcherFactory = consumerDispatcherFactory;
            this.eventBus = eventBus;
        }

        /// <inheritdoc />
        public IInternalConsumer CreateConsumer()
        {
            var dispatcher = consumerDispatcherFactory.GetConsumerDispatcher();
            return new InternalConsumer(connection, handlerRunner, dispatcher, conventions, eventBus);
        }

        /// <inheritdoc />
        public void OnDisconnected()
        {
            consumerDispatcherFactory.OnDisconnected();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            consumerDispatcherFactory.Dispose();
            handlerRunner.Dispose();
        }
    }
}
