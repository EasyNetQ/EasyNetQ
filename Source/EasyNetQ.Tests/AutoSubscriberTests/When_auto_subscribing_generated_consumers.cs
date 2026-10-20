using EasyNetQ.AutoSubscribe;
using EasyNetQ.Configuration;
using EasyNetQ.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_generated_consumers
{
    public sealed class MessageA;

    public sealed class Received
    {
        public readonly List<MessageA> Messages = new();
    }

    public sealed class ConsumerA(Received received) : IConsumeAsync<MessageA>
    {
        public Task ConsumeAsync(MessageA message, CancellationToken cancellationToken = default)
        {
            received.Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    public sealed class SyncConsumerA(Received received) : IConsume<MessageA>
    {
        public void Consume(MessageA message, CancellationToken cancellationToken = default) => received.Messages.Add(message);
    }

    private sealed class Source(params AutoSubscriberConsumer[] consumers) : IAutoSubscriberConsumerSource
    {
        public IReadOnlyList<AutoSubscriberConsumer> Consumers { get; } = consumers;
    }

    private static (IBus Bus, IPubSub PubSub, List<(string Id, Func<MessageA, CancellationToken, Task> OnMessage, Action<ISubscriptionConfiguration> Configure)> Calls) Bus(int failures = 0)
    {
        var pubSub = Substitute.For<IPubSub>();
        var bus = Substitute.For<IBus>();
        bus.PubSub.Returns(pubSub);
        var calls = new List<(string, Func<MessageA, CancellationToken, Task>, Action<ISubscriptionConfiguration>)>();
#pragma warning disable IDISP004
        pubSub.SubscribeAsync(Arg.Any<string>(), Arg.Any<Func<MessageA, CancellationToken, Task>>(), Arg.Any<Action<ISubscriptionConfiguration>>(), Arg.Any<CancellationToken>())
#pragma warning restore IDISP004
            .Returns(_ => failures-- > 0
                ? Task.FromException<SubscriptionResult>(new InvalidOperationException("broker down"))
                : Task.FromResult(new SubscriptionResult(new Topology.Exchange("x"), new Topology.Queue("q"), Substitute.For<IAsyncDisposable>())))
            .AndDoes(c => calls.Add(((string)c[0], (Func<MessageA, CancellationToken, Task>)c[1], (Action<ISubscriptionConfiguration>)c[2])));
        return (bus, pubSub, calls);
    }

    [Fact]
    public async Task Should_subscribe_with_the_generated_metadata_and_dispatch_through_DI()
    {
        var (bus, _, calls) = Bus();
        var services = new ServiceCollection();
        services.AddSingleton<Received>();
        services.AddTransient<ConsumerA>();
        await using var provider = services.BuildServiceProvider();
        var autoSubscriber = new AutoSubscriber(bus, provider, "app");

        await using var subscription = await autoSubscriber.SubscribeAsync(
            [
                AutoSubscriberConsumer.Async<MessageA, ConsumerA>(
                    new AutoSubscriberConsumerAttribute("explicit"),
                    ["a.one", "a.two"],
                    new SubscriptionConfigurationAttribute { PrefetchCount = 5, Expires = 100 })
            ],
            TestContext.Current.CancellationToken);

        var call = calls.Should().ContainSingle().Subject;
        call.Id.Should().Be("explicit");
        var configuration = new SubscriptionConfiguration(1);
        call.Configure(configuration);
        configuration.Topics.Should().Equal("a.one", "a.two");
        configuration.PrefetchCount.Should().Be(5);
        configuration.QueueArguments.Should().BeEquivalentTo(new Dictionary<string, object> { { Argument.Expires, 100 } });

        var message = new MessageA();
        await call.OnMessage(message, CancellationToken.None);
        provider.GetRequiredService<Received>().Messages.Should().Equal(message);
    }

    [Fact]
    public async Task Should_generate_the_subscription_id_and_default_topic_like_the_reflection_path()
    {
        var (bus, _, calls) = Bus();
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var autoSubscriber = new AutoSubscriber(bus, provider, "app");
        var generated = AutoSubscriberConsumer.Async<MessageA, ConsumerA>();

        await using var subscription = await autoSubscriber.SubscribeAsync([generated], TestContext.Current.CancellationToken);

        var configuration = new SubscriptionConfiguration(1);
        calls.Single().Configure(configuration);
        configuration.Topics.Should().Equal("#");
#pragma warning disable IL2026
        var reflected = new AutoSubscriberConsumerInfo(typeof(ConsumerA), typeof(IConsumeAsync<MessageA>), typeof(MessageA));
#pragma warning restore IL2026
        calls.Single().Id.Should().StartWith("app:").And.HaveLength("app:".Length + 32);
        reflected.ConcreteType.Should().Be(generated.Info.ConcreteType);
        reflected.Topics.Should().BeEquivalentTo(generated.Info.Topics);
    }

    [Fact]
    public async Task Should_subscribe_from_the_host_with_retries_and_report_through_the_status()
    {
        var (bus, _, calls) = Bus(failures: 2);
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(bus);
        services.AddSingleton<Received>();
        IEasyNetQBuilder builder = new EasyNetQBuilder(services);
        services.AddSingleton<IAutoSubscriberConsumerSource>(new Source(
            AutoSubscriberConsumer.Async<MessageA, ConsumerA>(),
            AutoSubscriberConsumer.Sync<MessageA, SyncConsumerA>()));
        builder.ConsumerHost(o => o.RetryDelay = TimeSpan.FromMilliseconds(10)).AutoSubscribe("app");
        await using var provider = services.BuildServiceProvider();

        var hosts = provider.GetServices<IHostedService>().ToList();
        var status = provider.GetRequiredService<IConsumerHostStatus>();
        foreach (var host in hosts) await host.StartAsync(TestContext.Current.CancellationToken);
        await status.WaitForStartedAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        status.LastError.Should().BeNull();
        status.PendingConsumers.Should().Be(0);
        calls.Should().HaveCount(4, "two failed attempts, then both consumers");
        provider.GetRequiredService<ConsumerA>().Should().NotBeNull("generated consumers are registered as transient");
        provider.GetRequiredService<SyncConsumerA>().Should().NotBeNull();

        await calls[^1].OnMessage(new MessageA(), CancellationToken.None);
        provider.GetRequiredService<Received>().Messages.Should().ContainSingle();
        foreach (var host in hosts) await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Should_refuse_when_no_generated_consumers_are_registered()
    {
        IEasyNetQBuilder builder = new EasyNetQBuilder(new ServiceCollection());

        var act = () => builder.AutoSubscribe("app");

        act.Should().Throw<EasyNetQException>().WithMessage("*no generated consumers*");
    }
}
