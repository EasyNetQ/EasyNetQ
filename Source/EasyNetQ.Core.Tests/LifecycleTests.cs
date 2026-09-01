using System.Collections.Concurrent;
using EasyNetQ.Configuration;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public class LifecycleTests
{
    public sealed record OrderPlaced(int Id);

    [Fact]
    public async Task Should_run_lifecycle_pipeline_for_connection_and_consumer_events()
    {
        var events = new ConcurrentQueue<(LifecycleLayer Layer, string Event, string? Queue)>();

        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(new InMemoryTransport());
        services.AddEasyNetQCore()
            .Lifecycle(lifecycle => lifecycle.Use("record", (context, next) =>
            {
                events.Enqueue((context.Layer, context.Event.Name, (context.Parent as ConsumerContext)?.Queue));
                return next(context);
            }))
            .Consume(consumer => consumer
                .Queue("lifecycle.q")
                .Handle<OrderPlaced>((_, _) => new ValueTask<AckDecision>(AckDecision.Ack))
            );

        await using var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        await hostedService.StopAsync(TestContext.Current.CancellationToken);

        events.Should().ContainInOrder(
            (LifecycleLayer.Connection, "Connected", null),
            (LifecycleLayer.Consumer, "Started", "lifecycle.q"),
            (LifecycleLayer.Consumer, "Stopped", "lifecycle.q")
        );
    }

    [Fact]
    public async Task Should_cost_nothing_when_no_lifecycle_step_is_registered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(new InMemoryTransport());
        services.AddEasyNetQCore()
            .Consume(consumer => consumer
                .Queue("quiet.q")
                .Handle<OrderPlaced>((_, _) => new ValueTask<AckDecision>(AckDecision.Ack))
            );

        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<LifecycleNotifier>().IsEnabled.Should().BeFalse();

        var hostedService = provider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        await hostedService.StopAsync(TestContext.Current.CancellationToken);
    }
}
