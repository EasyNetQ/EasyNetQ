// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public abstract class Given_a_PersistentConsumer
    {
        protected const string queueName = "my_queue";
        protected ConsumerConfiguration configuration;
        protected IConsumer consumer;
        protected int createConsumerCalled;
        protected IEventBus eventBus;
        protected IInternalConsumerFactory internalConsumerFactory;
        protected List<IInternalConsumer> internalConsumers;
        protected MockBuilder mockBuilder;
        protected MessageHandler onMessage;
        protected IPersistentConnection persistentConnection;
        protected IQueue queue;

        public Given_a_PersistentConsumer()
        {
            eventBus = new EventBus();
            internalConsumers = new List<IInternalConsumer>();

            createConsumerCalled = 0;
            mockBuilder = new MockBuilder();

            queue = new Queue(queueName, false);
            onMessage = (body, properties, info, cancellation) => Task.FromResult(AckStrategies.Ack);

            internalConsumerFactory = Substitute.For<IInternalConsumerFactory>();

            internalConsumerFactory.CreateConsumer().Returns(x =>
            {
                var internalConsumer = Substitute.For<IInternalConsumer>();
                internalConsumers.Add(internalConsumer);
                createConsumerCalled++;
                return internalConsumer;
            });
            configuration = new ConsumerConfiguration(0);
            consumer = new PersistentConsumer(
                queue,
                onMessage,
                configuration,
                internalConsumerFactory,
                eventBus
            );

            AdditionalSetup();
        }

        protected abstract void AdditionalSetup();
    }
}

// ReSharper restore InconsistentNaming
