using System;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Tests.ConsumerTests;

public abstract class Given_a_сonsumer : IDisposable
{
    private const string queueName = "my_queue";

    protected readonly IConsumer consumer;
    protected readonly IEventBus eventBus;
    protected readonly IInternalConsumerFactory internalConsumerFactory;
    protected readonly List<IInternalConsumer> internalConsumers;
    private bool disposed;

    protected Given_a_сonsumer()
    {
        eventBus = new EventBus(Substitute.For<ILogger<EventBus>>());
        internalConsumers = new List<IInternalConsumer>();

        var queue = new Queue(queueName, false);

        internalConsumerFactory = Substitute.For<IInternalConsumerFactory>();
#pragma warning disable IDISP004
        internalConsumerFactory.CreateConsumer(Arg.Any<ConsumerConfiguration>()).Returns(_ =>
#pragma warning restore IDISP004
        {
            var internalConsumer = Substitute.For<IInternalConsumer>();
            internalConsumers.Add(internalConsumer);
            internalConsumer.StartConsumingAsync(Arg.Any<bool>())
                .Returns(new InternalConsumerStatus(new[] { queue }, new[] { queue }, Array.Empty<Queue>()));
            return internalConsumer;
        });
        consumer = new Consumer.Consumer(
            Substitute.For<ILogger<Consumer.Consumer>>(),
            new ConsumerConfiguration(
                0,
                new Dictionary<Queue, PerQueueConsumerConfiguration>
                {
                    {
                        queue,
                        new PerQueueConsumerConfiguration(
                            false, "", false, null, _ => new(AckStrategies.AckAsync)
                        )
                    }
                }
            ),
            internalConsumerFactory,
            eventBus
        );
    }

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        consumer.Dispose();
    }
}
