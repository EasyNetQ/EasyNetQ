using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// https://www.rabbitmq.com/quorum-queues.html#dead-lettering

var completionTcs = new TaskCompletionSource<bool>();
Console.CancelKeyPress += (_, _) => completionTcs.TrySetResult(true);

var serviceCollection = new ServiceCollection();

serviceCollection.AddLogging(builder => builder.AddConsole());
serviceCollection.AddEasyNetQ("host=localhost")
    .UseNewtonsoftJson()
    .UseAlwaysAckConsumerErrorStrategy();

using var provider = serviceCollection.BuildServiceProvider();

var bus = provider.GetRequiredService<IBus>();
await bus.Advanced.QueueDeclareAsync(
    queue: "Events:Failed",
    arguments: new Dictionary<string, object>()
        .WithQueueType(QueueType.Quorum)
        .WithDeadLetterExchange(Exchange.DefaultName)
        .WithDeadLetterRoutingKey("Events")
        .WithMessageTtl(TimeSpan.FromSeconds(5))
        .WithOverflowType(OverflowType.RejectPublish)
        .WithDeadLetterStrategy(DeadLetterStrategy.AtLeastOnce)
);

var eventQueue = await bus.Advanced.QueueDeclareAsync(
    queue: "Events",
    arguments: new Dictionary<string, object>()
        .WithQueueType(QueueType.Quorum)
        .WithDeadLetterExchange(Exchange.DefaultName)
        .WithDeadLetterRoutingKey("Events:Failed")
        .WithOverflowType(OverflowType.RejectPublish)
        .WithDeadLetterStrategy(DeadLetterStrategy.AtLeastOnce)
);

await using var eventsConsumer = await bus.Advanced.ConsumeAsync(eventQueue, (_, _, _) => throw new Exception("Oops"));

await bus.Advanced.PublishAsync(Exchange.Default, "Events", true, true, MessageProperties.Empty, ReadOnlyMemory<byte>.Empty);

await completionTcs.Task;
