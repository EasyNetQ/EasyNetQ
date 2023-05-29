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
    queue: "Events:Failed",
    arguments: new Dictionary<string, object>()
        .WithQueueType(QueueType.Quorum)
        .WithQueueDeadLetterExchange(Exchange.DefaultName)
        .WithQueueDeadLetterRoutingKey("Events")
        .WithQueueMessageTtl(TimeSpan.FromSeconds(5))
        .WithQueueOverflowType(OverflowType.RejectPublish)
        .WithQueueDeadLetterStrategy(DeadLetterStrategy.AtLeastOnce)
);

var eventQueue = await bus.Advanced.QueueDeclareAsync(
    queue: "Events",
    arguments: new Dictionary<string, object>()
        .WithQueueType(QueueType.Quorum)
        .WithQueueDeadLetterExchange(Exchange.DefaultName)
        .WithQueueDeadLetterRoutingKey("Events:Failed")
        .WithQueueOverflowType(OverflowType.RejectPublish)
        .WithQueueDeadLetterStrategy(DeadLetterStrategy.AtLeastOnce)
);

using var eventsConsumer = bus.Advanced.Consume(eventQueue, (_, _, _) => throw new Exception("Oops"));

await bus.Advanced.PublishAsync(Exchange.Default, "Events", true, MessageProperties.Empty, ReadOnlyMemory<byte>.Empty);

await completionTcs.Task;
