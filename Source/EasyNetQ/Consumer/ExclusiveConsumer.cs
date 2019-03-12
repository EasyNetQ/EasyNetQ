using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class ExclusiveConsumer : IConsumer
    {
        private static readonly TimeSpan RestartConsumingPeriod = TimeSpan.FromSeconds(10);
        
        private readonly object syncLock = new object();
        private volatile bool isStarted;

        private readonly IQueue queue;
        private readonly Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;
        private readonly IConsumerConfiguration configuration;

        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;
  
        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers = new ConcurrentDictionary<IInternalConsumer, object>();

        private readonly IList<IDisposable> disposables = new List<IDisposable>();

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
        }

        public IDisposable StartConsuming()
        {
            disposables.Add(eventBus.Subscribe<ConnectionCreatedEvent>(e => ConnectionOnConnected()));
            disposables.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(e => ConnectionOnDisconnected()));
            disposables.Add(Timers.Start(s => StartConsumingInternal(), RestartConsumingPeriod, RestartConsumingPeriod));
            
            StartConsumingInternal();
            
            return new ConsumerCancellation(Dispose);   
        }

        private void StartConsumingInternal()
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
                {
                    isStarted = true;
                    eventBus.Publish(new StartConsumingSucceededEvent(this, queue));
                }
                else
                {
                    internalConsumer.Dispose();
                    internalConsumers.TryRemove(internalConsumer, out _);
                    eventBus.Publish(new StartConsumingFailedEvent(this, queue));
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
            StartConsumingInternal();
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;
            
            disposed = true;
            
            eventBus.Publish(new StoppedConsumingEvent(this));
            
            foreach (var disposal in disposables)
            {
                disposal.Dispose();
            }
            
            foreach (var internalConsumer in internalConsumers.Keys)
            {
                internalConsumer.Dispose();
            }
        }
    }
}