using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Whole-stack numbers against a real broker. Opt-in: set <c>EASYNETQ_BENCH_RABBIT</c> to a connection string
///     (e.g. <c>host=localhost</c>); otherwise this class is filtered out by <c>Program</c>.
/// </summary>
[MemoryDiagnoser]
public class EndToEndRabbitBenchmarks
{
    private ServiceProvider provider = null!;
    private IBus bus = null!;
    private SubscriptionResult subscription;
    private TaskCompletionSource received = new();
    private SmallMessage publishOnlyMessage = null!;
    private RoundTripMessage roundTripMessage = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var connectionString = Environment.GetEnvironmentVariable("EASYNETQ_BENCH_RABBIT")
            ?? throw new InvalidOperationException("Set EASYNETQ_BENCH_RABBIT to a connection string, e.g. host=localhost");

        var services = new ServiceCollection();
        services.AddEasyNetQ(connectionString);
        provider = services.BuildServiceProvider();
        bus = provider.GetRequiredService<IBus>();

        subscription = await bus.PubSub.SubscribeAsync<RoundTripMessage>(
            "benchmark",
            (_, _) =>
            {
                received.TrySetResult();
                return Task.CompletedTask;
            },
            _ => { }
        );

        publishOnlyMessage = SampleMessages.CreateSmall();
        roundTripMessage = new RoundTripMessage { Id = 1, Name = "Test" };

        // warm up: declares topology, opens channels, drains any leftovers from a previous run
        await bus.PubSub.PublishAsync(publishOnlyMessage);
        await PublishAndConsume();
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await subscription.DisposeAsync();
        await provider.DisposeAsync();
    }

    /// <summary>Publish to an exchange nobody consumes from (fire-and-forget without confirms).</summary>
    [Benchmark]
    public Task Publish() => bus.PubSub.PublishAsync(publishOnlyMessage);

    /// <summary>Publish and wait until the subscriber's handler has run.</summary>
    [Benchmark]
    public async Task PublishAndConsume()
    {
        received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await bus.PubSub.PublishAsync(roundTripMessage);
        await received.Task;
    }
}

public class RoundTripMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
