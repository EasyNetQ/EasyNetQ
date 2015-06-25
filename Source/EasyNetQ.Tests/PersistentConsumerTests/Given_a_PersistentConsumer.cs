// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    [TestFixture]
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
        
        [SetUp]
        public void SetUp()
        {
            eventBus = new EventBus();
            internalConsumers = new List<IInternalConsumer>();

            createConsumerCalled = 0;
            mockBuilder = new MockBuilder();

            queue = new Queue(queueName, false);
            onMessage = (body, properties, info) => Task.Factory.StartNew(() => { });

            persistentConnection = MockRepository.GenerateStub<IPersistentConnection>();

            internalConsumerFactory = MockRepository.GenerateStub<IInternalConsumerFactory>();
            
            internalConsumerFactory.Stub(x => x.CreateConsumer()).WhenCalled(x =>
                {
                    var internalConsumer = MockRepository.GenerateStub<IInternalConsumer>();
                    internalConsumers.Add(internalConsumer);
                    createConsumerCalled++;
                    x.ReturnValue = internalConsumer;
                }).Repeat.Any();
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