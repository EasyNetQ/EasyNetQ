using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class ExclusiveConsumer : IConsumer
    {
        private readonly object syncLock = new object();
        private volatile bool isStarted;

        private readonly IQueue queue;
        private readonly Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;
        private readonly IConsumerConfiguration configuration;

        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;
  
        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers = new ConcurrentDictionary<IInternalConsumer, object>();

        private readonly IList<CancelSubscription> eventCancellations = new List<CancelSubscription>();

        public ExclusiveConsumer(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IPersistentConnection connection,
            IConsumerConfiguration configuration,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus
            )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(configuration, "configuration");
       
            this.queue = queue;
            this.onMessage = onMessage;
            this.connection = connection;
            this.configuration = configuration;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
            timer = new Timer(s =>
                {
                    StartConsumer();
                    ((Timer)s).Change(10000, -1);
                });
            timer.Change(10000, -1);
        }

        public IDisposable StartConsuming()
        {
            eventCancellations.Add(eventBus.Subscribe<ConnectionCreatedEvent>(e => ConnectionOnConnected()));
            eventCancellations.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(e => ConnectionOnDisconnected()));
            StartConsumer();
            return new ConsumerCancellation(Dispose);   
        }

        private void StartConsumer()
        {
            if (disposed)
                return;
            if (!connection.IsConnected)
                return;

            lock (syncLock)
            {
                if (isStarted)
                    return;
                var internalConsumer = internalConsumerFactory.CreateConsumer();
                internalConsumers.TryAdd(internalConsumer, null);
                internalConsumer.Cancelled += consumer => Dispose();
                var status = internalConsumer.StartConsuming(connection, queue, onMessage, configuration);
                if (status == StartConsumingStatus.Succeed)
                    isStarted = true;
                else
                {
                    internalConsumer.Dispose();
                    object value;
                    internalConsumers.TryRemove(internalConsumer, out value);
                }
            }
        }

        private void ConnectionOnDisconnected()
        {
            lock (syncLock)
            {
                isStarted = false;
                internalConsumers.Clear();
                internalConsumerFactory.OnDisconnected();
            }
        }

        private void ConnectionOnConnected()
        {
            StartConsumer();
        }

        private bool disposed = false;
        private readonly Timer timer;

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            timer.Dispose();
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