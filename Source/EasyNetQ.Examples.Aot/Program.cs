using System.Text;
using System.Text.Json.Serialization;
using EasyNetQ;
using EasyNetQ.Configuration;
using EasyNetQ.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Native AOT smoke test: publish with PublishAot, run against a broker, exit 0 when every check passes.
// Covers the fluent v9 API end to end: background consumer start that retries until a missing exchange exists,
// type-level and per-route wire names, aliases, [MessageType], HandleUnknown for foreign types, untyped messages,
// case-insensitive JSON through a source-generated context, and a quorum error queue.
// Usage: EasyNetQ.Examples.Aot [connectionString]   (default: host=localhost)
static ReadOnlyMemory<byte> Raw(string json) => Encoding.UTF8.GetBytes(json);
var connectionString = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("EASYNETQ_CONNECTION") ?? "host=localhost";
var run = Guid.NewGuid().ToString("N")[..8];
var exchange = $"aot.smoke.{run}";
var typedQueue = $"aot.smoke.{run}.typed";
var untypedQueue = $"aot.smoke.{run}.untyped";

var ping = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
var pong = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
var contract = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
var legacy = new TaskCompletionSource<Ping>(TaskCreationOptions.RunContinuationsAsynchronously);
var foreign = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
var untyped = new TaskCompletionSource<Ping>(TaskCreationOptions.RunContinuationsAsynchronously);
var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddEasyNetQ(connectionString)
    .UseSystemTextJson(AotJsonContext.Default)
    .MessageType<Ping>(m => m.WireName("aot.ping.v1").Alias("Legacy.Ping"))
    // a consumer of the per-route contract name below knows it as an alias
    .MessageType<Pong>(m => m.Alias("aot.pong.v1"))
    .ConsumerHost(o => o.RetryDelay = TimeSpan.FromMilliseconds(200))
    .UseRabbitMq(r => r
        .ErrorQueue(q => q.Quorum())
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
            })));

await using var provider = services.BuildServiceProvider();
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var host = provider.GetServices<IHostedService>().Single();
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
await host.StartAsync(timeout.Token);
await Task.Delay(500, timeout.Token);
Check("non-blocking start while the exchange is missing", !status.IsStarted, $"(pending {status.PendingConsumers})");
await advanced.ExchangeDeclareAsync(exchange, ExchangeType.Topic, cancellationToken: timeout.Token);
await status.WaitForStartedAsync(timeout.Token);
Check("consumers started once the exchange exists", status.IsStarted);

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
var errorQueue = await advanced.GetQueueStatsAsync("EasyNetQ_Default_Error_Queue", timeout.Token);
Check("failed message reached the (quorum) error queue", errorQueue.MessagesCount > 0, $"({errorQueue.MessagesCount} messages)");

await host.StopAsync(CancellationToken.None);
await advanced.ExchangeDeleteAsync(exchange, cancellationToken: CancellationToken.None);
Console.WriteLine(failures == 0 ? "ALL OK" : $"{failures} FAILED");
return failures == 0 ? 0 : 1;

public sealed record Ping(Guid Id, string Text);

public sealed record Pong(int Id);

[MessageType("aot.contract.v1", Aliases = ["Old.Contract"])]
public sealed record Contract(int Id);

[JsonSerializable(typeof(Ping))]
[JsonSerializable(typeof(Pong))]
[JsonSerializable(typeof(Contract))]
internal sealed partial class AotJsonContext : JsonSerializerContext;
