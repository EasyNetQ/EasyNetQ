// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public abstract class Given_a_PersistentConsumer
    {
        protected MockBuilder mockBuilder;
        protected IConsumer consumer;
        protected List<IInternalConsumer> internalConsumers;
        protected IInternalConsumerFactory internalConsumerFactory;
        protected Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        protected IQueue queue;
        protected IPersistentConnection persistentConnection;
        protected IEventBus eventBus;
        protected IConsumerConfiguration configuration;

        protected const string queueName = "my_queue";
        protected int createConsumerCalled;
        
        public Given_a_PersistentConsumer()
        {
            eventBus = new EventBus();
            internalConsumers = new List<IInternalConsumer>();

            createConsumerCalled = 0;
            mockBuilder = new MockBuilder();

            queue = new Queue(queueName, false);
            onMessage = (body, properties, info) => Task.Factory.StartNew(() => { });

            persistentConnection = Substitute.For<IPersistentConnection>();
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
                persistentConnection,
                configuration,
                internalConsumerFactory, 
                eventBus);

            AdditionalSetup();
        }

        public abstract void AdditionalSetup();
    }
}

// ReSharper restore InconsistentNaming