using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

var serviceCollection = new ServiceCollection();

serviceCollection.AddEasyNetQ("host=localhost;publisherConfirms=True");
serviceCollection.AddLogging(builder => builder.AddConsole());
serviceCollection.EnableNewtonsoftJson();
serviceCollection.EnableAlwaysNackWithoutRequeueConsumerErrorStrategy();

var provider = serviceCollection.BuildServiceProvider();

var bus = provider.GetRequiredService<IBus>();
var eventQueue = await bus.Advanced.QueueDeclareAsync(
    queue: "Events",
    arguments: new Dictionary<string, object>()
        .WithQueueType(QueueType.Quorum)
        .WithOverflowType(OverflowType.RejectPublish),
    cancellationToken: cts.Token
);

using var eventsConsumer = bus.Advanced.Consume(eventQueue, (_, _, _) => { });

while (!cts.IsCancellationRequested)
{
    try
    {
        await bus.Advanced.PublishAsync(
            Exchange.Default,
            "Events",
            true,
            true,
            MessageProperties.Empty,
            ReadOnlyMemory<byte>.Empty,
            cts.Token
        );
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception)
    {
        await Task.Delay(5000, cts.Token);
    }
}
