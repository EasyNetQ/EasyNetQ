using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;

namespace EasyNetQ.Consumer
{
    /// <inheritdoc />
    public class ConsumerFactory : IConsumerFactory
    {
        private readonly ConcurrentSet<IConsumer> consumers = new ConcurrentSet<IConsumer>();
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;

        /// <summary>
        ///     Creates ConsumerFactory
        /// </summary>
        public ConsumerFactory(IPersistentConnection connection, IEventBus eventBus, IHandlerRunner handlerRunner)
        {
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));
            Preconditions.CheckNotNull(handlerRunner, nameof(handlerRunner));

            this.connection = connection;
            this.eventBus = eventBus;
            this.handlerRunner = handlerRunner;

            eventBus.Subscribe<StoppedConsumingEvent>(@event => consumers.Remove(@event.Consumer));
        }

        /// <inheritdoc />
        public IConsumer CreateConsumer(
            IQueue queue, MessageHandler onMessage, ConsumerConfiguration configuration
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");

            var consumer = CreateConsumerInstance(queue, onMessage, configuration);
            consumers.Add(consumer);
            return consumer;
        }

        /// <inheritdoc />
        public IConsumer CreateConsumer(
            IReadOnlyCollection<Tuple<IQueue, MessageHandler>> queueConsumerPairs, ConsumerConfiguration configuration
        )
        {
            throw new NotSupportedException("Exclusive multiple consuming is not supported.");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var consumer in consumers)
                consumer.Dispose();
        }

        /// <summary>
        ///     Create the correct implementation of IConsumer based on queue properties
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="onMessage"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private IConsumer CreateConsumerInstance(
            IQueue queue, MessageHandler onMessage, ConsumerConfiguration configuration
        )
        {
            return new Consumer(connection, queue, onMessage, configuration, eventBus, handlerRunner);
        }
    }
}
