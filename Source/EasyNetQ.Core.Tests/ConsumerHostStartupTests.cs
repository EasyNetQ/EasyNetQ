using System.Collections.Concurrent;
using EasyNetQ.Configuration;
using EasyNetQ.Hosting;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public class ConsumerHostStartupTests
{
    public sealed record FileEvent(long FileId);

    private static ServiceProvider Build(StrictTopologyTransport transport, Action<IEasyNetQBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(transport);
        configure(services.AddEasyNetQCore().ConsumerHost(o => o.RetryDelay = TimeSpan.FromMilliseconds(20)));
        return services.BuildServiceProvider();
    }

    private static Action<IEasyNetQBuilder> BindingConsumer(TaskCompletionSource<FileEvent> received)
        => builder => builder.Consume(c => c
            .Queue("vloer.nextcloud-events")
            .BindExisting("nextcloud.events", "file.#")
            .Handle<FileEvent>((message, _) =>
            {
                received.TrySetResult(message);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            }));

    [Fact]
    public async Task Should_start_in_the_background_and_retry_until_the_exchange_exists()
    {
        var received = new TaskCompletionSource<FileEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new StrictTopologyTransport();
        await using var provider = Build(transport, BindingConsumer(received));
        var host = provider.GetServices<IHostedService>().Single();
        var status = provider.GetRequiredService<IConsumerHostStatus>();

        var start = host.StartAsync(TestContext.Current.CancellationToken);
        start.IsCompleted.Should().BeTrue("startup must not block the host while the topology is missing");

        while (status.LastError is null)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        status.IsStarted.Should().BeFalse();
        status.PendingConsumers.Should().Be(1);
        status.LastError.Message.Should().Contain("nextcloud.events");

        transport.DeclareExternally("nextcloud.events");
        await status.WaitForStartedAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        status.LastError.Should().BeNull();

        await WireNameTests.PublishRawAsync(transport.Inner, provider, "nextcloud.events", "file.deleted", null, new FileEvent(7));
        (await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Should().Be(new FileEvent(7));
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_retry_while_the_broker_is_unreachable()
    {
        var received = new TaskCompletionSource<FileEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new StrictTopologyTransport { FailConnects = 3 };
        transport.DeclareExternally("nextcloud.events");
        await using var provider = Build(transport, BindingConsumer(received));
        var host = provider.GetServices<IHostedService>().Single();

        await host.StartAsync(TestContext.Current.CancellationToken);
        await provider.GetRequiredService<IConsumerHostStatus>().WaitForStartedAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        transport.FailConnects.Should().Be(0);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_block_startup_until_running_when_asked()
    {
        var received = new TaskCompletionSource<FileEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new StrictTopologyTransport { FailConnects = 2 };
        transport.DeclareExternally("nextcloud.events");
        await using var provider = Build(transport, builder => BindingConsumer(received)(builder.ConsumerHost(o => o.WaitForStartup = true)));
        var host = provider.GetServices<IHostedService>().Single();

        await host.StartAsync(TestContext.Current.CancellationToken);

        provider.GetRequiredService<IConsumerHostStatus>().IsStarted.Should().BeTrue();
        provider.GetRequiredService<ConsumerHostOptions>().RetryDelay.Should().Be(TimeSpan.FromMilliseconds(20), "ConsumerHost(...) calls configure one options instance");
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_report_failed_starts_through_the_lifecycle_pipeline()
    {
        var events = new ConcurrentQueue<string>();
        var transport = new StrictTopologyTransport();
        await using var provider = Build(transport, builder => builder
            .Lifecycle(l => l.Use("record", (context, next) =>
            {
                events.Enqueue(context.Event.Name);
                return next(context);
            }))
            .Consume(c => c
                .Queue("q")
                .BindExisting("missing", "#")
                .Handle<FileEvent>((_, _) => new ValueTask<AckDecision>(AckDecision.Ack))));
        var host = provider.GetServices<IHostedService>().Single();

        await host.StartAsync(TestContext.Current.CancellationToken);
        while (!events.Contains(LifecycleEvent.StartFailed.Name))
            await Task.Delay(10, TestContext.Current.CancellationToken);

        await host.StopAsync(TestContext.Current.CancellationToken);
        provider.GetRequiredService<IConsumerHostStatus>().IsStarted.Should().BeFalse();
    }
}
