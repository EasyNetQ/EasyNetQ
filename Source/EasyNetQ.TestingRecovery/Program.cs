using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Loggers;
using EasyNetQ.Topology;

namespace EasyNetQ.TestingRecovery
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus("host=localhost:5673,localhost:5674",
                                            s => s.Register<IEasyNetQLogger>(p => new ConsoleLogger())
                                                  .Register<IConsumerFactory>(p => new MyConsumerFactory(p.Resolve<IInternalConsumerFactory>(), p.Resolve<IEventBus>())));

            SpinWait.SpinUntil(() => bus.IsConnected);

            bus.Respond<MyRequest, MyResponse>(request => new MyResponse(), config => config.WithDurable(false));

            while (true)
            {
                try
                {
                    var response = bus.Request<MyRequest, MyResponse>(new MyRequest());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                Thread.Sleep(1000);
            }

            bus.Dispose();
        }
    }

    public class MyConsumerFactory : IConsumerFactory
    {
        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;

        private readonly ConcurrentDictionary<IConsumer, object> consumers = new ConcurrentDictionary<IConsumer, object>();

        public MyConsumerFactory(IInternalConsumerFactory internalConsumerFactory, IEventBus eventBus)
        {
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;

            eventBus.Subscribe<StoppedConsumingEvent>(stoppedConsumingEvent =>
            {
                object value;
                consumers.TryRemove(stoppedConsumingEvent.Consumer, out value);
            });
        }

        public IConsumer CreateConsumer(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IPersistentConnection connection,
            IConsumerConfiguration configuration
            )
        {
            var consumer = CreateConsumerInstance(queue, onMessage, connection, configuration);
            consumers.TryAdd(consumer, null);
            return consumer;
        }

        /// <summary>
        /// Create the correct implementation of IConsumer based on queue properties
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="onMessage"></param>
        /// <param name="connection"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private IConsumer CreateConsumerInstance(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IPersistentConnection connection,
            IConsumerConfiguration configuration)
        {
            if (queue.IsExclusive && configuration.RecoveryAction == null)
            {
                return new TransientConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
            }
            if (configuration.IsExclusive)
                return new ExclusiveConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
            return new PersistentRecoveryOnCancelConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
        }

        public void Dispose()
        {
            foreach (var consumer in consumers.Keys)
            {
                consumer.Dispose();
            }
            internalConsumerFactory.Dispose();
        }
    }

    public class PersistentRecoveryOnCancelConsumer : IConsumer
    {
        private readonly IQueue queue;
        private readonly Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;
        private readonly IConsumerConfiguration configuration;

        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;

        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers =
            new ConcurrentDictionary<IInternalConsumer, object>();

        private readonly IList<CancelSubscription> eventCancellations = new List<CancelSubscription>();

        private bool shouldRecover = false;

        public PersistentRecoveryOnCancelConsumer(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IPersistentConnection connection,
            IConsumerConfiguration configuration,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus)
        {
            this.queue = queue;
            this.onMessage = onMessage;
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

            if (shouldRecover && configuration.RecoveryAction != null)
            {
                try
                {
                    configuration.RecoveryAction();
                }
                catch (Exception ex)
                {
                    // TODO: do not eat the exception here, avoid the recovery action on durable queue!!
                    Trace.WriteLine(ex.ToString());
                }
            }
            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.TryAdd(internalConsumer, null);

            internalConsumer.Cancelled += consumer => Dispose();
            internalConsumer.CancelledByBroker += OnCancelledByBroker;

            internalConsumer.StartConsuming(
                connection,
                queue,
                onMessage,
                configuration
                );
        }

        private void ConnectionOnDisconnected()
        {
            internalConsumerFactory.OnDisconnected();
            internalConsumers.Clear();
            shouldRecover = true; //we need only recovery in case of connection loss, it is not needed to set it subsequently false
        }

        private void OnCancelledByBroker(IInternalConsumer internalConsumer)
        {
            shouldRecover = true; //we need only recovery in case of connection loss, it is not needed to set it subsequently false
            object temp;
            //We should dispose current internal consumer, as pending messages' ack/nack may shutdown it with unknown delivery tag
            internalConsumers.TryRemove(internalConsumer, out temp);
            internalConsumer.Dispose();
            StartConsumingInternal();
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

    public class MyMessage
    {
        public Guid Id { get; set; }
        public string Data { get; set; }
    }

    public class MyRequest
    {
    }

    public class MyResponse
    {
    }
}