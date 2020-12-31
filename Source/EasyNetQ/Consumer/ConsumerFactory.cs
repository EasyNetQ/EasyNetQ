using System;
using System.Collections.Concurrent;
using EasyNetQ.Events;
using EasyNetQ.Internals;

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
        private readonly IEventBus eventBus;
        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IDisposable unsubscribeFromStoppedConsumerEvent;

        /// <summary>
        ///     Creates ConsumerFactory
        /// </summary>
        /// <param name="internalConsumerFactory">The internal consumer factory</param>
        /// <param name="eventBus">The event bus</param>
        public ConsumerFactory(
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(internalConsumerFactory, nameof(internalConsumerFactory));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));

            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;

            unsubscribeFromStoppedConsumerEvent = eventBus.Subscribe<StoppedConsumingEvent>(
                @event => consumers.Remove(@event.Consumer.Id)
            );
        }

        /// <inheritdoc />
        public IConsumer CreateConsumer(ConsumerConfiguration configuration)
        {
            var consumer = new Consumer(configuration, internalConsumerFactory, eventBus);
            consumers.TryAdd(consumer.Id, consumer);
            return consumer;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            unsubscribeFromStoppedConsumerEvent.Dispose();
            consumers.ClearAndDispose();
        }
    }
}
