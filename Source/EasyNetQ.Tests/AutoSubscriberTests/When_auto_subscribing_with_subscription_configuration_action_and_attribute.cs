using EasyNetQ.AutoSubscribe;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_with_subscription_configuration_action_and_attribute
{
    private readonly IBus bus;
    private Action<ISubscriptionConfiguration> capturedAction;
    private readonly IPubSub pubSub;

    public When_auto_subscribing_with_subscription_configuration_action_and_attribute()
    {
        pubSub = Substitute.For<IPubSub>();
        bus = Substitute.For<IBus>();
        bus.PubSub.Returns(pubSub);

        var autoSubscriber = new AutoSubscriber(bus, "my_app")
        {
            ConfigureSubscriptionConfiguration =
                c => c.WithAutoDelete(false)
                    .WithExpires(11)
                    .WithPrefetchCount(11)
                    .WithPriority(11)
        };

        pubSub.SubscribeAsync(
                Arg.Is("MyActionAndAttributeTest"),
                Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>()
            )
            .Returns(Task.FromResult(new SubscriptionResult()))
            .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

        autoSubscriber.Subscribe(new[] { typeof(MyConsumerWithActionAndAttribute) });
    }

    [Fact]
    public void Should_have_called_subscribe()
    {
        pubSub.Received().SubscribeAsync(
            Arg.Any<string>(),
            Arg.Any<Func<MessageA, CancellationToken, Task>>(),
            Arg.Any<Action<ISubscriptionConfiguration>>()
        );
    }

    [Fact]
    public void Should_have_called_subscribe_with_attribute_values_notaction_values()
    {
        var subscriptionConfiguration = new SubscriptionConfiguration(1);

        capturedAction(subscriptionConfiguration);

        subscriptionConfiguration.AutoDelete.Should().BeTrue();
        subscriptionConfiguration.PrefetchCount.Should().Be(10);
        subscriptionConfiguration.Priority.Should().Be(10);
        subscriptionConfiguration.QueueArguments.Should().BeEquivalentTo(new Dictionary<string, object> { { "x-expires", 10 } });
    }

    // Discovered by reflection over test assembly, do not remove.
    private class MyConsumerWithActionAndAttribute : IConsume<MessageA>
    {
        [AutoSubscriberConsumer(SubscriptionId = "MyActionAndAttributeTest")]
        [SubscriptionConfiguration(AutoDelete = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
        public void Consume(MessageA message, CancellationToken cancellationToken)
        {
        }
    }

    private class MessageA
    {
    }
}
