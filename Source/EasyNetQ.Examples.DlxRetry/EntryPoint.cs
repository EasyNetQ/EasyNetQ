using EasyNetQ;
using EasyNetQ.Topology;

// https://www.rabbitmq.com/quorum-queues.html#dead-lettering

var completionTcs = new TaskCompletionSource<bool>();
Console.CancelKeyPress += (_, _) => completionTcs.TrySetResult(true);

using var bus = RabbitHutch.CreateBus(
    "host=localhost",
    x => x.EnableConsoleLogger()
        .EnableNewtonsoftJson()
        .EnableAlwaysNackWithoutRequeueConsumerErrorStrategy()
);

await bus.Advanced.QueueDeclareAsync(
    "Events:Failed",
    c => c.WithQueueType(QueueType.Quorum)
        .WithDeadLetterExchange(Exchange.Default)
        .WithDeadLetterRoutingKey("Events")
        .WithMessageTtl(TimeSpan.FromSeconds(5)) // A fixed delay between retry attempts
        .WithOverflowType(OverflowType.RejectPublish)
        .WithDeadLetterStrategy(DeadLetterStrategy.AtLeastOnce)
);

var eventQueue = await bus.Advanced.QueueDeclareAsync(
    "Events",
    c => c.WithQueueType(QueueType.Quorum)
        .WithDeadLetterExchange(Exchange.Default)
        .WithDeadLetterRoutingKey("Events:Failed")
        .WithOverflowType(OverflowType.RejectPublish)
        .WithDeadLetterStrategy(DeadLetterStrategy.AtLeastOnce)
);

using var eventsConsumer = bus.Advanced.Consume(eventQueue, (_, _, _) => throw new Exception("Oops"));

await bus.Advanced.PublishAsync(Exchange.Default, "Events", true, true, new MessageProperties(), ReadOnlyMemory<byte>.Empty);

await completionTcs.Task;
