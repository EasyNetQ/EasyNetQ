// ReSharper disable InconsistentNaming
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Internals;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_async_with_subscription_configuration_action
{
    private readonly IBus bus;
    private Action<ISubscriptionConfiguration> capturedAction;
    private readonly IPubSub pubSub;

    public When_auto_subscribing_async_with_subscription_configuration_action()
    {
        pubSub = Substitute.For<IPubSub>();
        bus = Substitute.For<IBus>();
        bus.PubSub.Returns(pubSub);

        var autoSubscriber = new AutoSubscriber(bus, "my_app")
        {
            ConfigureSubscriptionConfiguration =
                c => c.WithAutoDelete()
                    .WithExpires(10)
                    .WithPrefetchCount(10)
                    .WithPriority(10)
        };

        pubSub.SubscribeAsync(
                Arg.Is("MyActionTest"),
                Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>()
            )
            .Returns(Task.FromResult(new SubscriptionResult()).ToAwaitableDisposable())
            .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

        autoSubscriber.Subscribe(new[] { typeof(MyConsumerWithAction) });
    }

    [Fact]
    public void Should_have_called_subscribe_async()
    {
        pubSub.Received().SubscribeAsync(
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
        subscriptionConfiguration.Expires.Should().Be(10);
        subscriptionConfiguration.PrefetchCount.Should().Be(10);
        subscriptionConfiguration.Priority.Should().Be(10);
    }

    // Discovered by reflection over test assembly, do not remove.
    // ReSharper disable once UnusedMember.Local
    private class MyConsumerWithAction : IConsumeAsync<MessageA>
    {
        [AutoSubscriberConsumer(SubscriptionId = "MyActionTest")]
        public Task ConsumeAsync(MessageA message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class MessageA
    {
    }
}

// ReSharper restore InconsistentNaming
