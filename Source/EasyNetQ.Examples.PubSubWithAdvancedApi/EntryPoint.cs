using EasyNetQ;
using EasyNetQ.Topology;

// https://www.rabbitmq.com/quorum-queues.html#dead-lettering

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

using var bus = RabbitHutch.CreateBus(
    "host=localhost;publisherConfirms=True",
    x => x.EnableNewtonsoftJson()
        .EnableAlwaysNackWithoutRequeueConsumerErrorStrategy()
);

var eventQueue = await bus.Advanced.QueueDeclareAsync(
    "Events",
    c => c.WithQueueType(QueueType.Quorum)
        .WithOverflowType(OverflowType.RejectPublish),
    cts.Token
);

using var eventsConsumer = bus.Advanced.Consume(eventQueue, (_, _, _) => { });

while (!cts.IsCancellationRequested)
{
    await bus.Advanced.PublishAsync(
        Exchange.Default,
        "Events",
        true,
        new MessageProperties(),
        ReadOnlyMemory<byte>.Empty,
        cts.Token
    );
}
