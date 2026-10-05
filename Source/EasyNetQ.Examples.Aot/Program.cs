using System.Text;
using System.Text.Json.Serialization;
using EasyNetQ;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Configuration;
using EasyNetQ.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Native AOT smoke test: publish with PublishAot, run against a broker, exit 0 when every check passes.
// Covers the fluent v9 API end to end: background consumer start that retries until a missing exchange exists,
// type-level and per-route wire names, aliases, [MessageType], HandleUnknown for foreign types, untyped messages,
// case-insensitive JSON through a source-generated context, and a named quorum error queue. Then the 8.x-compatible
// IBus API: PubSub with [Exchange]/[Queue], Rpc, SendReceive, the DLX+TTL scheduler, and the AutoSubscriber on
// generated registrations (DI-resolved IConsume/IConsumeAsync consumers with [ForTopic], [AutoSubscriberConsumer]
// and [SubscriptionConfiguration]).
// Usage: EasyNetQ.Examples.Aot [connectionString]   (default: host=localhost)
static byte[] Raw(string json) => Encoding.UTF8.GetBytes(json);
var connectionString = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("EASYNETQ_CONNECTION") ?? "host=localhost";
var run = Guid.NewGuid().ToString("N")[..8];
var exchange = $"aot.smoke.{run}";
var typedQueue = $"aot.smoke.{run}.typed";
var untypedQueue = $"aot.smoke.{run}.untyped";
const string errorQueueName = "aot.smoke.errors";

var ping = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
var pong = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
var contract = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
var legacy = new TaskCompletionSource<Ping>(TaskCreationOptions.RunContinuationsAsynchronously);
var foreign = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
var untyped = new TaskCompletionSource<Ping>(TaskCreationOptions.RunContinuationsAsynchronously);
var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

var shipments = new Shipments();
var services = new ServiceCollection();
services.AddSingleton(shipments);
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddEasyNetQ(connectionString)
    .UseSystemTextJson(AotJsonContext.Default)
    .MessageType<Ping>(m => m.WireName("aot.ping.v1").Alias("Legacy.Ping"))
    // a consumer of the per-route contract name below knows it as an alias
    .MessageType<Pong>(m => m.Alias("aot.pong.v1"))
    .ConsumerHost(o => o.RetryDelay = TimeSpan.FromMilliseconds(200))
    .UseRabbitMq(r => r
        .ErrorQueue(errorQueueName, q => q.Quorum())
        .Publish(p => p
            .Exchange(exchange, e => e.Topic())
            .Message<Ping>("ping")
            .Message<Pong>("pong", route => route.WireName("aot.pong.v1"))
            .Message<Contract>("contract"))
        .Consume(c => c
            .Queue(typedQueue, q => q.AutoDelete())
            .BindExisting(exchange, "#")
            .Handle<Ping>((message, context) =>
            {
                if (message.Text == "fail")
                {
                    failed.TrySetResult();
                    throw new InvalidOperationException("routes this message to the error queue");
                }
                if (context.Properties.Type == "Legacy.Ping") legacy.TrySetResult(message);
                if (context.Properties.Type == "aot.ping.v1") ping.TrySetResult(context.Properties.Type);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            })
            .Handle<Pong>((_, context) =>
            {
                pong.TrySetResult(context.Properties.Type);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            })
            .Handle<Contract>((_, context) =>
            {
                contract.TrySetResult(context.Properties.Type);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            })
            .HandleUnknown((body, context) =>
            {
                foreign.TrySetResult($"{context.Properties.Type}:{Encoding.UTF8.GetString(body.Span)}");
                return new ValueTask<AckDecision>(AckDecision.Ack);
            }))
        .Consume(c => c
            .Queue(untypedQueue, q => q.AutoDelete())
            .Handle<Ping>((message, _) =>
            {
                untyped.TrySetResult(message);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            })))
    .AutoSubscribe(run, o => o.Configure = autoSubscriber =>
        autoSubscriber.ConfigureSubscriptionConfiguration = c => c.WithAutoDelete());

await using var provider = services.BuildServiceProvider();
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var hosts = provider.GetServices<IHostedService>().ToList();
var status = provider.GetRequiredService<IConsumerHostStatus>();
var advanced = provider.GetRequiredService<IBus>().Advanced;
var publisher = provider.GetRequiredService<IMessagePublisher>();
var failures = 0;

void Check(string name, bool ok, string detail = "")
{
    Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {name} {detail}");
    if (!ok) failures++;
}

// the consumer binds to an exchange nobody declared yet: startup must neither block nor crash
foreach (var host in hosts) await host.StartAsync(timeout.Token);
await Task.Delay(500, timeout.Token);
Check("non-blocking start while the exchange is missing", !status.IsStarted, $"(pending {status.PendingConsumers})");
await advanced.ExchangeDeclareAsync(exchange, ExchangeType.Topic, cancellationToken: timeout.Token);
await status.WaitForStartedAsync(timeout.Token);
Check("consumers started once the exchange exists", status.IsStarted);
var generatedConsumers = provider.GetServices<IAutoSubscriberConsumerSource>().Sum(s => s.Consumers.Count);
Check("generated auto-subscriber consumers", generatedConsumers == 2, $"({generatedConsumers})");

await publisher.PublishAsync(new Ping(Guid.NewGuid(), "hello"), timeout.Token);
await publisher.PublishAsync(new Pong(1), timeout.Token);
await publisher.PublishAsync(new Contract(7), timeout.Token);
await advanced.PublishAsync(exchange, "legacy", false, null, new MessageProperties { Type = "Legacy.Ping" },
    Raw($"{{\"id\":\"{Guid.NewGuid()}\",\"text\":\"camelCase\"}}"), timeout.Token);
await advanced.PublishAsync(exchange, "foreign", false, null, new MessageProperties { Type = "Tracer.Plugins.Slack.Events.SlackChat" },
    Raw("{\"text\":\"hi\"}"), timeout.Token);
await advanced.PublishAsync("", untypedQueue, false, null, new MessageProperties(),
    Raw($"{{\"id\":\"{Guid.NewGuid()}\",\"text\":\"untyped\"}}"), timeout.Token);
await publisher.PublishAsync(new Ping(Guid.NewGuid(), "fail"), timeout.Token);

Check("type wire name", await ping.Task.WaitAsync(timeout.Token) == "aot.ping.v1");
Check("per-route wire name", await pong.Task.WaitAsync(timeout.Token) == "aot.pong.v1");
Check("[MessageType] wire name", await contract.Task.WaitAsync(timeout.Token) == "aot.contract.v1");
Check("alias + case-insensitive JSON", (await legacy.Task.WaitAsync(timeout.Token)).Text == "camelCase");
Check("HandleUnknown for a foreign type", await foreign.Task.WaitAsync(timeout.Token) == "Tracer.Plugins.Slack.Events.SlackChat:{\"text\":\"hi\"}");
Check("untyped message to the only handler", (await untyped.Task.WaitAsync(timeout.Token)).Text == "untyped");
await failed.Task.WaitAsync(timeout.Token);
await Task.Delay(1000, timeout.Token);
var errorQueue = await advanced.GetQueueStatsAsync(errorQueueName, timeout.Token);
Check("failed message reached the named (quorum) error queue", errorQueue.MessagesCount > 0, $"({errorQueue.MessagesCount} messages)");

// 8.x-compatible IBus API: PubSub with [Exchange]/[Queue] naming, Rpc, SendReceive and the DLX+TTL scheduler
var bus = provider.GetRequiredService<IBus>();
var published = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);
var scheduled = new TaskCompletionSource<Reminder>(TaskCreationOptions.RunContinuationsAsynchronously);
var sent = new TaskCompletionSource<Command>(TaskCreationOptions.RunContinuationsAsynchronously);
await using (await bus.PubSub.SubscribeAsync<OrderPlaced>(run, (m, _) => { published.TrySetResult(m); return Task.CompletedTask; },
    c => c.WithTopic("order.#").WithAutoDelete(), timeout.Token))
await using (await bus.PubSub.SubscribeAsync<Reminder>(run, (m, _) => { scheduled.TrySetResult(m); return Task.CompletedTask; },
    c => c.WithAutoDelete(), timeout.Token))
await using (await bus.Rpc.RespondAsync<Question, Answer>((q, _) => Task.FromResult(new Answer(q.Value * 2)),
    c => c.WithQueueName($"aot.compat.rpc.{run}").WithExpires(60_000), timeout.Token))
await using (await bus.SendReceive.ReceiveAsync($"aot.compat.commands.{run}", r => r.Add<Command>((m, _) => { sent.TrySetResult(m); return Task.CompletedTask; }),
    c => c.WithAutoDelete(), timeout.Token))
{
    await bus.PubSub.PublishAsync(new OrderPlaced(42), "order.eu", timeout.Token);
    Check("PubSub publish/subscribe via [Exchange]/[Queue]", (await published.Task.WaitAsync(timeout.Token)).Id == 42);
    var answer = await bus.Rpc.RequestAsync<Question, Answer>(new Question(21), c => c.WithQueueName($"aot.compat.rpc.{run}"), timeout.Token);
    Check("Rpc request/respond", answer.Value == 42);
    await bus.SendReceive.SendAsync($"aot.compat.commands.{run}", new Command("go"), timeout.Token);
    Check("SendReceive send/receive", (await sent.Task.WaitAsync(timeout.Token)).Name == "go");
    var futureAt = DateTime.UtcNow;
    await bus.Scheduler.FuturePublishAsync(new Reminder(run), TimeSpan.FromSeconds(1), timeout.Token);
    var reminder = await scheduled.Task.WaitAsync(timeout.Token);
    Check("Scheduler future publish (DLX + TTL)", reminder.Run == run && DateTime.UtcNow - futureAt >= TimeSpan.FromMilliseconds(900));
}

var shipmentsBus = provider.GetRequiredService<IBus>();
await shipmentsBus.PubSub.PublishAsync(new ShipmentDispatched(1, "eu"), "shipment.eu", timeout.Token);
await shipmentsBus.PubSub.PublishAsync(new ShipmentDispatched(2, "us"), "shipment.us", timeout.Token);
await shipments.Audited.Task.WaitAsync(timeout.Token);
var eu = await shipments.Europe.Task.WaitAsync(timeout.Token);
await Task.Delay(500, timeout.Token);
Check("AutoSubscriber IConsumeAsync with [ForTopic] and a DI dependency", eu.Id == 1 && shipments.EuropeCount == 1, $"({shipments.EuropeCount} eu)");
Check("AutoSubscriber IConsume on the default topic", shipments.AuditCount == 2, $"({shipments.AuditCount} audited)");
Check("[AutoSubscriberConsumer] subscription id names the queue", shipments.EuropeQueue == "aot.compat.shipments_eu", shipments.EuropeQueue ?? "");

foreach (var host in hosts) await host.StopAsync(CancellationToken.None);
await advanced.ExchangeDeleteAsync(exchange, cancellationToken: CancellationToken.None);
Console.WriteLine(failures == 0 ? "ALL OK" : $"{failures} FAILED");
return failures == 0 ? 0 : 1;

public sealed record Ping(Guid Id, string Text);

public sealed record Pong(int Id);

[MessageType("aot.contract.v1", Aliases = ["Old.Contract"])]
public sealed record Contract(int Id);

[Exchange("aot.compat.orders")]
[Queue("aot.compat.orders")]
public sealed record OrderPlaced(int Id);

[Exchange("aot.compat.reminders")]
[Queue("aot.compat.reminders")]
public sealed record Reminder(string Run);

[Exchange("aot.compat.shipments")]
[Queue("aot.compat.shipments")]
public sealed record ShipmentDispatched(int Id, string Region);

public sealed class Shipments
{
    public readonly TaskCompletionSource<ShipmentDispatched> Europe = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public readonly TaskCompletionSource Audited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int EuropeCount;
    public int AuditCount;
    public string? EuropeQueue;
}

// resolved from DI per message; the generator reads the attributes at compile time
public sealed class EuropeanShipments(Shipments shipments, IBus bus) : IConsumeAsync<ShipmentDispatched>
{
    [AutoSubscriberConsumer(SubscriptionId = "eu")]
    [ForTopic("shipment.eu")]
    [SubscriptionConfiguration(PrefetchCount = 2, AutoDelete = true)]
    public async Task ConsumeAsync(ShipmentDispatched message, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref shipments.EuropeCount);
        // throws when the subscription id did not name the queue
        await bus.Advanced.QueueDeclarePassiveAsync("aot.compat.shipments_eu", cancellationToken);
        shipments.EuropeQueue = "aot.compat.shipments_eu";
        shipments.Europe.TrySetResult(message);
    }
}

public sealed class ShipmentAudit(Shipments shipments) : IConsume<ShipmentDispatched>
{
    public void Consume(ShipmentDispatched message, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref shipments.AuditCount) == 2) shipments.Audited.TrySetResult();
    }
}

public sealed record Question(int Value);

public sealed record Answer(int Value);

public sealed record Command(string Name);

[JsonSerializable(typeof(ShipmentDispatched))]
[JsonSerializable(typeof(OrderPlaced))]
[JsonSerializable(typeof(Reminder))]
[JsonSerializable(typeof(Question))]
[JsonSerializable(typeof(Answer))]
[JsonSerializable(typeof(Command))]
[JsonSerializable(typeof(Ping))]
[JsonSerializable(typeof(Pong))]
[JsonSerializable(typeof(Contract))]
internal sealed partial class AotJsonContext : JsonSerializerContext;
