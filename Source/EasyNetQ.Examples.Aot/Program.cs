using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Minimal publish/subscribe round trip used as the Native AOT smoke test.
// Usage: EasyNetQ.Examples.Aot [connectionString]   (default: host=localhost)
var connectionString = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("EASYNETQ_CONNECTION") ?? "host=localhost";

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddEasyNetQ(connectionString).UseSystemTextJson();

await using var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBus>();

var received = new TaskCompletionSource<Ping>(TaskCreationOptions.RunContinuationsAsynchronously);
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

await using var subscription = await bus.PubSub.SubscribeAsync<Ping>(
    "aot-smoke",
    (ping, _) =>
    {
        received.TrySetResult(ping);
        return Task.CompletedTask;
    },
    _ => { },
    timeout.Token
);

var sent = new Ping(Guid.NewGuid(), DateTime.UtcNow);
await bus.PubSub.PublishAsync(sent, timeout.Token);

var ping = await received.Task.WaitAsync(timeout.Token);
Console.WriteLine(ping.Id == sent.Id ? $"OK: round trip for {ping.Id}" : $"MISMATCH: sent {sent.Id}, received {ping.Id}");
return ping.Id == sent.Id ? 0 : 1;

public sealed record Ping(Guid Id, DateTime SentAt);
