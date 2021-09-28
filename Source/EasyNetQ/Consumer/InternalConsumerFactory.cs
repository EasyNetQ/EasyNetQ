using System;

namespace EasyNetQ.Consumer
{
    /// <inheritdoc />
    public interface IInternalConsumerFactory : IDisposable
    {
        /// <summary>
        ///     Creates consumer based on configuration 
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IInternalConsumer CreateConsumer(ConsumerConfiguration configuration);
    }

    /// <inheritdoc />
    public class InternalConsumerFactory : IInternalConsumerFactory
    {
        private readonly IConsumerConnection connection;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;

        /// <summary>
        ///     Creates InternalConsumerFactory
        /// </summary>
        public InternalConsumerFactory(
            IConsumerConnection connection, IHandlerRunner handlerRunner, IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(handlerRunner, nameof(handlerRunner));

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
