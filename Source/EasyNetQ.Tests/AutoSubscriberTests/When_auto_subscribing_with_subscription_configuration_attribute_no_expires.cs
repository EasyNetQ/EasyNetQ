using EasyNetQ.AutoSubscribe;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_with_subscription_configuration_attribute_no_expires : IDisposable
{
    private readonly IBus bus;
    private readonly ServiceProvider serviceProvider;
    private Action<ISubscriptionConfiguration> capturedAction;
    private readonly IPubSub pubSub;
    private bool disposed;

    public When_auto_subscribing_with_subscription_configuration_attribute_no_expires()
    {
        pubSub = Substitute.For<IPubSub>();
        bus = Substitute.For<IBus>();
        bus.PubSub.Returns(pubSub);

        var services = new ServiceCollection();
        serviceProvider = services.BuildServiceProvider();

        var autoSubscriber = new AutoSubscriber(bus, serviceProvider, "my_app");

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
        autoSubscriber.Subscribe([typeof(MyConsumerWithAttr)]);
#pragma warning restore IDISP004
    }

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
    public void Should_have_called_subscribe_with_no_expires()
    {
        var subscriptionConfiguration = new SubscriptionConfiguration(1);

        capturedAction(subscriptionConfiguration);

        subscriptionConfiguration.AutoDelete.Should().BeTrue();
        subscriptionConfiguration.PrefetchCount.Should().Be(10);
        subscriptionConfiguration.Priority.Should().Be(10);
        subscriptionConfiguration.QueueArguments.Should().BeNull();
    }

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        serviceProvider?.Dispose();
    }

    // Discovered by reflection over test assembly, do not remove.
    private sealed class MyConsumerWithAttr : IConsume<MessageA>
    {
        [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
        [SubscriptionConfiguration(AutoDelete = true, PrefetchCount = 10, Priority = 10)]
        public void Consume(MessageA message, CancellationToken cancellationToken)
        {
        }
    }

    private sealed class MessageA
    {
    }
}
