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
        private static readonly ConcurrentDictionary<string, object> ExclusiveQueueArea = 
            new ConcurrentDictionary<string, object>(); 

        private readonly IQueue queue;
        private readonly Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;
        private readonly IConsumerConfiguration configuration;

        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;
  
        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers =
            new ConcurrentDictionary<IInternalConsumer, object>();

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
                    if (disposed)
                        return;
                    StartConsumingInternal();
                    ((Timer) s).Change(5000, Timeout.Infinite);
                });
            timer.Change(5000, Timeout.Infinite);
        }

        public IDisposable StartConsuming()
        {
            eventCancellations.Add(eventBus.Subscribe<ConnectionCreatedEvent>(e => ConnectionOnConnected()));
            eventCancellations.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(e => ConnectionOnDisconnected()));
            return new ConsumerCancellation(Dispose);   
        }


        private void StartConsumingInternal()
        {
            if (disposed) return;
            if (!connection.IsConnected) return;
            if (TryEnterExclusiveArea(queue))
            {
                var internalConsumer = internalConsumerFactory.CreateConsumer();
                internalConsumers.TryAdd(internalConsumer, null);
                internalConsumer.Cancelled += consumer => Dispose();
                var status = internalConsumer.StartConsuming(connection, queue, onMessage, configuration);
                if (status == StartConsumingStatus.Failed)
                    LeaveExclusiveArea(queue);
            }
        }

        private void ConnectionOnDisconnected()
        {
            internalConsumerFactory.OnDisconnected();
            internalConsumers.Clear();
            LeaveExclusiveArea(queue);
        }

        private void ConnectionOnConnected()
        {
            StartConsumingInternal();
        }

        private static bool TryEnterExclusiveArea(IQueue queue)
        {
            return ExclusiveQueueArea.TryAdd(queue.Name, null);
        }

        private static void LeaveExclusiveArea(IQueue queue)
        {
            object value;
            ExclusiveQueueArea.TryRemove(queue.Name, out value);
        }

        private bool disposed = false;
        private readonly Timer timer;
        
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            timer.Dispose();
            LeaveExclusiveArea(queue);
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