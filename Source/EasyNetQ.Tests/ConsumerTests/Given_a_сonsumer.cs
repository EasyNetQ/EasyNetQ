using EasyNetQ.Consumer;
using MS = Microsoft.Extensions.Logging;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumerTests;

public abstract class Given_a_сonsumer
{
    private const string queueName = "my_queue";

    protected readonly IConsumer consumer;
    protected readonly IEventBus eventBus;
    protected readonly IInternalConsumerFactory internalConsumerFactory;
    protected readonly List<IInternalConsumer> internalConsumers;

    protected Given_a_сonsumer()
    {
        eventBus = new EventBus(Substitute.For<MS.ILogger<EventBus>>());
        internalConsumers = new List<IInternalConsumer>();

        var queue = new Queue(queueName, false);

        internalConsumerFactory = Substitute.For<IInternalConsumerFactory>();
        internalConsumerFactory.CreateConsumer(Arg.Any<ConsumerConfiguration>()).Returns(_ =>
        {
            var internalConsumer = Substitute.For<IInternalConsumer>();
            internalConsumers.Add(internalConsumer);
            internalConsumer.StartConsuming(Arg.Any<bool>())
                .Returns(new InternalConsumerStatus(new[] { queue }, new[] { queue }, Array.Empty<Queue>()));
            return internalConsumer;
        });
        consumer = new Consumer.Consumer(
            Substitute.For<MS.ILogger<Consumer.Consumer>>(),
            new ConsumerConfiguration(
                0,
                new Dictionary<Queue, PerQueueConsumerConfiguration>
                {
                    {
                        queue,
                        new PerQueueConsumerConfiguration(
                            false, "", false, null, _ => new(AckStrategies.Ack)
                        )
                    }
                }
            ),
            internalConsumerFactory,
            eventBus
        );
    }
}
