using System;

namespace EasyNetQ.Consumer
{
    public interface IInternalConsumerFactory : IDisposable
    {
        IInternalConsumer CreateConsumer(ConsumerConfiguration configuration);
    }

    /// <inheritdoc />
    public class InternalConsumerFactory : IInternalConsumerFactory
    {
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;

        public InternalConsumerFactory(
            IPersistentConnection connection, IHandlerRunner handlerRunner, IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");

            this.connection = connection;
            this.handlerRunner = handlerRunner;
            this.eventBus = eventBus;
        }

        /// <inheritdoc />
        public IInternalConsumer CreateConsumer(ConsumerConfiguration configuration)
        {
            return new InternalConsumer(configuration, connection, handlerRunner, eventBus);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
