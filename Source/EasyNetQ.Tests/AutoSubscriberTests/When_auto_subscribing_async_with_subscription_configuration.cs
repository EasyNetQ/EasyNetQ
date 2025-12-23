using EasyNetQ.AutoSubscribe;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_async_with_subscription_configuration_attribute : IDisposable, IAsyncLifetime
{
    private readonly IBus bus;
    private readonly ServiceProvider serviceProvider;
    private Action<ISubscriptionConfiguration> capturedAction;
    private readonly IPubSub pubSub;
    private bool disposed;
    readonly AutoSubscriber autoSubscriber;
    public When_auto_subscribing_async_with_subscription_configuration_attribute()
    {
        pubSub = Substitute.For<IPubSub>();
        bus = Substitute.For<IBus>();
        bus.PubSub.Returns(pubSub);

        var services = new ServiceCollection();
        serviceProvider = services.BuildServiceProvider();

        autoSubscriber = new AutoSubscriber(bus, serviceProvider, "my_app");

#pragma warning disable IDISP004
        pubSub.SubscribeAsync(
#pragma warning restore IDISP004
                Arg.Is("MyAttrTest"),
                Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>()
            )
            .Returns(Task.FromResult(new SubscriptionResult()))
            .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

#pragma warning disable IDISP004

#pragma warning restore IDISP004
    }
    public async Task InitializeAsync()
    {
        await autoSubscriber.SubscribeAsync([typeof(MyConsumerWithAttr)]);
    }

    public Task DisposeAsync() => Task.CompletedTask;
    [Fact]
    public void Should_have_called_subscribe()
    {
#pragma warning disable IDISP004
        pubSub.Received().SubscribeAsync(
#pragma warning restore IDISP004
            Arg.Any<string>(),
            Arg.Any<Func<MessageA, CancellationToken, Task>>(),
            Arg.Any<Action<ISubscriptionConfiguration>>()
        );
    }

    [Fact]
    public void Should_have_called_subscribe_with_action_capable_of_configuring_subscription()
    {
        var subscriptionConfiguration = new SubscriptionConfiguration(1);

        capturedAction.Should().NotBeNull("SubscribeAsync should have been invoked");
        capturedAction(subscriptionConfiguration);

        subscriptionConfiguration.AutoDelete.Should().BeTrue();
        subscriptionConfiguration.PrefetchCount.Should().Be(10);
        subscriptionConfiguration.Priority.Should().Be(10);
        subscriptionConfiguration.QueueArguments.Should().BeEquivalentTo(new Dictionary<string, object> { { Argument.Expires, 10 } });
    }

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        serviceProvider?.Dispose();
    }

    // Discovered by reflection over test assembly, do not remove.
    private sealed class MyConsumerWithAttr : IConsumeAsync<MessageA>
    {
        [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
        [SubscriptionConfiguration(AutoDelete = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
        public Task ConsumeAsync(MessageA message, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class MessageA
    {
    }
}

public class When_auto_subscribing_async_explicit_implementation_with_subscription_configuration_attribute : IDisposable, IAsyncLifetime
{
    private readonly IBus bus;
    private readonly ServiceProvider serviceProvider;
    private Action<ISubscriptionConfiguration> capturedAction;
    private readonly IPubSub pubSub;
    private bool disposed;
    readonly AutoSubscriber autoSubscriber;
    public When_auto_subscribing_async_explicit_implementation_with_subscription_configuration_attribute()
    {
        pubSub = Substitute.For<IPubSub>();
        bus = Substitute.For<IBus>();
        bus.PubSub.Returns(pubSub);

        var services = new ServiceCollection();
        serviceProvider = services.BuildServiceProvider();

        autoSubscriber = new AutoSubscriber(bus, serviceProvider, "my_app");

#pragma warning disable IDISP004
        pubSub.SubscribeAsync(
#pragma warning restore IDISP004
                Arg.Is("MyAttrTest"),
                Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>()
            )
            .Returns(Task.FromResult(new SubscriptionResult()))
            .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

#pragma warning disable IDISP004
#pragma warning restore IDISP004
    }
    public async Task InitializeAsync()
    {
        await autoSubscriber.SubscribeAsync([typeof(MyConsumerWithAttr)]);
    }

    public Task DisposeAsync() => Task.CompletedTask;
    [Fact]
    public void Should_have_called_subscribe()
    {
#pragma warning disable IDISP004
        pubSub.Received().SubscribeAsync(
#pragma warning restore IDISP004
            Arg.Any<string>(),
            Arg.Any<Func<MessageA, CancellationToken, Task>>(),
            Arg.Any<Action<ISubscriptionConfiguration>>()
        );
    }

    [Fact]
    public void Should_have_called_subscribe_with_action_capable_of_configuring_subscription()
    {
        var subscriptionConfiguration = new SubscriptionConfiguration(1);

        capturedAction.Should().NotBeNull("SubscribeAsync should have been invoked");
        capturedAction(subscriptionConfiguration);

        subscriptionConfiguration.AutoDelete.Should().BeTrue();
        subscriptionConfiguration.PrefetchCount.Should().Be(10);
        subscriptionConfiguration.Priority.Should().Be(10);
        subscriptionConfiguration.QueueArguments.Should().BeEquivalentTo(new Dictionary<string, object> { { Argument.Expires, 10 } });
    }

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        serviceProvider?.Dispose();
    }

    // Discovered by reflection over test assembly, do not remove.
    private sealed class MyConsumerWithAttr : IConsumeAsync<MessageA>
    {
        [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
        [SubscriptionConfiguration(AutoDelete = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
        Task IConsumeAsync<MessageA>.ConsumeAsync(MessageA message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class MessageA
    {
    }
}
