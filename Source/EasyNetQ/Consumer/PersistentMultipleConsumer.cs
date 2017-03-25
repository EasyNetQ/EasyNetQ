﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class PersistentMultipleConsumer : IConsumer
    {
        private readonly ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> _queueConsumerPairs;
        private readonly IPersistentConnection connection;
        private readonly IConsumerConfiguration configuration;

        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;

        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers =
            new ConcurrentDictionary<IInternalConsumer, object>();

        private readonly IList<CancelSubscription> eventCancellations = new List<CancelSubscription>();

        public PersistentMultipleConsumer(
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs,
            IPersistentConnection connection,
            IConsumerConfiguration configuration,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(queueConsumerPairs, nameof(queueConsumerPairs));
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(internalConsumerFactory, nameof(internalConsumerFactory));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            _queueConsumerPairs = queueConsumerPairs;
            this.connection = connection;
            this.configuration = configuration;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
        }

        public IDisposable StartConsuming()
        {
            eventCancellations.Add(eventBus.Subscribe<ConnectionCreatedEvent>(e => ConnectionOnConnected()));
            eventCancellations.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(e => ConnectionOnDisconnected()));

            StartConsumingInternal();

            return new ConsumerCancellation(Dispose);
        }

        private void StartConsumingInternal()
        {
            if (disposed) return;

            if (!connection.IsConnected)
            {
                // connection is not connected, so just ignore this call. A consumer will
                // be created and start consuming when the connection reconnects.
                return;
            }

            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.TryAdd(internalConsumer, null);

            internalConsumer.Cancelled += consumer => Dispose();

            internalConsumer.StartConsuming(
                connection,
                _queueConsumerPairs,
                configuration);
        }

        private void ConnectionOnDisconnected()
        {
            internalConsumerFactory.OnDisconnected();
            internalConsumers.Clear();
        }

        private void ConnectionOnConnected()
        {
            StartConsumingInternal();
        }

        private bool disposed = false;

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            eventBus.Publish(new StoppedConsumingEvent(this));

            foreach (var cancelSubscription in eventCancellations)
            {
                cancelSubscription();
            }

            foreach (var internalConsumer in internalConsumers.Keys)
            {
                internalConsumer.Dispose();
            }
        }
    }
}