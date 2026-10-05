using EasyNetQ.Configuration;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Tests;

/// <summary>The 8.x IBus API over the in-memory transport, as apps test it</summary>
public class InMemoryBusTests
{
    [Exchange("tests.reminders")]
    [Queue("tests.reminders")]
    public sealed record Reminder(string Text);

    public sealed record Question(int Value);

    public sealed record Answer(int Value);

    public sealed class Observed
    {
        public int Events;
    }

    private static ServiceProvider Build(InMemoryTransport transport, bool withLifecycleStep = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<Observed>();
        var builder = services.AddEasyNetQ("host=localhost;timeout=5");
        if (withLifecycleStep)
            builder.Lifecycle(l => l.Use("observe", (context, next) =>
            {
                // a step that resolves services, as readiness checks do
                Interlocked.Increment(ref context.Services.GetRequiredService<Observed>().Events);
                return next(context);
            }));
        services.RemoveAll<ITransport>();
        services.AddSingleton<ITransport>(transport);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_deliver_a_future_publish_after_the_delay_through_the_DLX_scheduler()
    {
        var transport = new InMemoryTransport();
        await using var provider = Build(transport);
        var bus = provider.GetRequiredService<IBus>();
        var received = new TaskCompletionSource<(Reminder, DateTime)>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var subscription = await bus.PubSub.SubscribeAsync<Reminder>("app", (m, _) =>
        {
            received.TrySetResult((m, DateTime.UtcNow));
            return Task.CompletedTask;
        }, _ => { }, TestContext.Current.CancellationToken);

        var published = DateTime.UtcNow;
        await bus.Scheduler.FuturePublishAsync(new Reminder("tea"), TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
        transport.Broker.MessageCount("tests.reminders_00_00_00_00").Should().Be(1, "the message waits in the delay queue");

        var (reminder, at) = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reminder.Text.Should().Be("tea");
        (at - published).Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(250));
        transport.Broker.MessageCount("tests.reminders_00_00_00_00").Should().Be(0);
    }

    [Fact]
    public async Task Should_dispose_the_container_after_rpc_without_lifecycle_steps_resolving_disposed_services()
    {
        var transport = new InMemoryTransport();
        var provider = Build(transport, withLifecycleStep: true);
        var bus = provider.GetRequiredService<IBus>();
        await using (await bus.Rpc.RespondAsync<Question, Answer>((q, _) => Task.FromResult(new Answer(q.Value * 2)), _ => { }, TestContext.Current.CancellationToken))
        {
            (await bus.Rpc.RequestAsync<Question, Answer>(new Question(21), TestContext.Current.CancellationToken)).Value.Should().Be(42);
        }

        var dispose = async () => await provider.DisposeAsync();

        await dispose.Should().NotThrowAsync();
    }
}
