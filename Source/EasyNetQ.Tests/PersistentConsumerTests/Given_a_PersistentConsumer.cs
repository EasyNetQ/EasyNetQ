// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public abstract class Given_a_PersistentConsumer
    {
        private const string queueName = "my_queue";

        protected readonly IConsumer consumer;
        protected readonly IEventBus eventBus;
        protected readonly IInternalConsumerFactory internalConsumerFactory;
        protected readonly List<IInternalConsumer> internalConsumers;

        protected Given_a_PersistentConsumer()
        {
            eventBus = new EventBus();
            internalConsumers = new List<IInternalConsumer>();

            Queue queue = new Queue(queueName, false);
            MessageHandler handler = (body, properties, info, cancellation) => Task.FromResult(AckStrategies.Ack);

            internalConsumerFactory = Substitute.For<IInternalConsumerFactory>();
            internalConsumerFactory.CreateConsumer(Arg.Any<ConsumerConfiguration>()).Returns(x =>
            {
                var internalConsumer = Substitute.For<IInternalConsumer>();
                internalConsumers.Add(internalConsumer);
                internalConsumer.StartConsuming(Arg.Any<bool>())
                    .Returns(new InternalConsumerStatus(new[] { queue }, Array.Empty<Queue>()));
                return internalConsumer;
            });
            consumer = new Consumer.Consumer(
                new ConsumerConfiguration(
                    0,
                    new Dictionary<Queue, PerQueueConsumerConfiguration>
                    {
                        {queue, new PerQueueConsumerConfiguration("", false, null, handler)}
                    }
                ),
                internalConsumerFactory,
                eventBus
            );
        }
    }
}

// ReSharper restore InconsistentNaming
