using System.Runtime.InteropServices;
using System.Text;
using EasyNetQ;
using EasyNetQ.Topology;
using Serilog;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

using var bus = RabbitHutch.CreateBus(
    "host=localhost;port=5672;publisherConfirms=True",
    x => x.EnableNewtonsoftJson()
        .EnableAlwaysNackWithoutRequeueConsumerErrorStrategy()
        .Register(typeof(ILogger), _ => Log.Logger)
        .EnableSerilogLogging()
);

var eventQueue = await bus.Advanced.QueueDeclareAsync(
    "Events",
    c => c.WithQueueType(QueueType.Quorum)
        .WithOverflowType(OverflowType.RejectPublish),
    cts.Token
);

using var eventsConsumer = bus.Advanced.Consume<bool>(
    eventQueue,
    (message, _) =>
    {
        Log.Logger.Information(message.Body.ToString());
    });

while (!cts.IsCancellationRequested)
{
    try
    {
        await bus.Advanced.PublishAsync(
            Exchange.Default,
            "Events",
            true,
            new MessageProperties(),
            "true"u8.ToArray(),
            cts.Token
        );
        await Task.Delay(5000, cts.Token);
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception exception)
    {
        Log.Logger.Error(exception, "Failed to publish");
        await Task.Delay(5000, cts.Token);
    }
}
