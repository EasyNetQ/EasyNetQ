using EasyNetQ.Events;
using EasyNetQ.Internals;
using System;
using System.Collections.Concurrent;

namespace EasyNetQ.Consumer
{
    public interface IConsumerFactory : IDisposable
    {
        IConsumer CreateConsumer(ConsumerConfiguration configuration);
    }

    /// <inheritdoc />
    public class ConsumerFactory : IConsumerFactory
    {
        private readonly ConcurrentDictionary<Guid, IConsumer> consumers = new ConcurrentDictionary<Guid, IConsumer>();

        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;
        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IDisposable unsubscribeFromStoppedConsumerEvent;

        /// <summary>
        ///     Creates ConsumerFactory
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="internalConsumerFactory">The internal consumer factory</param>
        /// <param name="eventBus">The event bus</param>
        public ConsumerFactory(
            IPersistentConnection connection,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(internalConsumerFactory, nameof(internalConsumerFactory));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));

            this.connection = connection;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;

            unsubscribeFromStoppedConsumerEvent = eventBus.Subscribe<StoppedConsumingEvent>(
                @event => consumers.Remove(@event.Consumer.Id)
            );
        }

        /// <inheritdoc />
        public IConsumer CreateConsumer(ConsumerConfiguration configuration)
        {
            return new Consumer(configuration, connection, internalConsumerFactory, eventBus);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            unsubscribeFromStoppedConsumerEvent.Dispose();
            internalConsumerFactory.Dispose();
            consumers.ClearAndDispose();
        }
    }
}
