using EasyNetQ.AutoSubscribe;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_async_with_subscription_configuration_action_and_attribute : IDisposable, IAsyncLifetime
{
    private readonly IBus bus;
    private readonly ServiceProvider serviceProvider;
    private Action<ISubscriptionConfiguration> capturedAction;
    private readonly IPubSub pubSub;
    private bool disposed;
    AutoSubscriber autoSubscriber;
    public When_auto_subscribing_async_with_subscription_configuration_action_and_attribute()
    {
        pubSub = Substitute.For<IPubSub>();
        bus = Substitute.For<IBus>();
        bus.PubSub.Returns(pubSub);

        var services = new ServiceCollection();
        serviceProvider = services.BuildServiceProvider();

        autoSubscriber = new AutoSubscriber(bus, serviceProvider, "my_app")
        {
            ConfigureSubscriptionConfiguration =
                c => c.WithAutoDelete(false)
                    .WithExpires(11)
                    .WithPrefetchCount(11)
                    .WithPriority(11)
        };

#pragma warning disable IDISP004
        pubSub.SubscribeAsync(
#pragma warning restore IDISP004
                Arg.Is("MyActionAndAttributeTest"),
                Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>()
            )
            .Returns(Task.FromResult(new SubscriptionResult()))
            .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

    }

    [Fact]
    public void Should_have_called_subscribe_async()
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
    public void Should_have_called_subscribe_async_with_attribute_values_notaction_values()
    {
        var subscriptionConfiguration = new SubscriptionConfiguration(1);

        capturedAction.Should().NotBeNull("SubscribeAsync should have been invoked");

        capturedAction(subscriptionConfiguration);

        subscriptionConfiguration.AutoDelete.Should().BeTrue();
        subscriptionConfiguration.PrefetchCount.Should().Be(10);
        subscriptionConfiguration.Priority.Should().Be(10);
        subscriptionConfiguration.QueueArguments.Should().BeEquivalentTo(new Dictionary<string, object> { { "x-expires", 10 } });
    }

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        serviceProvider?.Dispose();
    }

    public Task InitializeAsync() => autoSubscriber.SubscribeAsync([typeof(MyConsumerWithActionAndAttribute)]);

    public Task DisposeAsync() => Task.CompletedTask;

    // Discovered by reflection over test assembly, do not remove.
    // ReSharper disable once UnusedMember.Local
    private sealed class MyConsumerWithActionAndAttribute : IConsumeAsync<MessageA>
    {
        [AutoSubscriberConsumer(SubscriptionId = "MyActionAndAttributeTest")]
        [SubscriptionConfiguration(AutoDelete = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
        public Task ConsumeAsync(MessageA message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class MessageA
    {
    }
}
