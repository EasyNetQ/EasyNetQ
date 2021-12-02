using System;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer
{
    /// <inheritdoc />
    public interface IInternalConsumerFactory : IDisposable
    {
        /// <summary>
        ///     Creates a consumer based on the configuration 
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IInternalConsumer CreateConsumer(ConsumerConfiguration configuration);
    }

    /// <inheritdoc />
    public class InternalConsumerFactory : IInternalConsumerFactory
    {
        private readonly ILogger<InternalConsumer> logger;
        private readonly IConsumerConnection connection;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;

        /// <summary>
        ///     Creates InternalConsumerFactory
        /// </summary>
        public InternalConsumerFactory(
            ILogger<InternalConsumer> logger,
            IConsumerConnection connection,
            IHandlerRunner handlerRunner,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(logger, nameof(logger));
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(handlerRunner, nameof(handlerRunner));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));

            this.logger = logger;
            this.connection = connection;
            this.handlerRunner = handlerRunner;
            this.eventBus = eventBus;
        }

        /// <inheritdoc />
        public IInternalConsumer CreateConsumer(ConsumerConfiguration configuration)
        {
            return new InternalConsumer(logger, configuration, connection, handlerRunner, eventBus);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
