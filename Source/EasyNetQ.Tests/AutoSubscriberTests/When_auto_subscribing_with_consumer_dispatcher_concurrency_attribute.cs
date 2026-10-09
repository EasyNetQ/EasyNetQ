using EasyNetQ.AutoSubscribe;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_with_consumer_dispatcher_concurrency_attribute : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IPubSub pubSub;
    private readonly AutoSubscriber autoSubscriber;
    private Action<ISubscriptionConfiguration> capturedAction;
    private bool disposed;

    public When_auto_subscribing_with_consumer_dispatcher_concurrency_attribute()
    {
        pubSub = Substitute.For<IPubSub>();
        var bus = Substitute.For<IBus>();
        bus.PubSub.Returns(pubSub);

        serviceProvider = new ServiceCollection().BuildServiceProvider();

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
    }

    [Fact]
    public void Should_have_called_subscribe_with_consumer_dispatcher_concurrency()
    {
        var subscriptionConfiguration = new SubscriptionConfiguration(1);

        capturedAction(subscriptionConfiguration);

        subscriptionConfiguration.ConsumerDispatcherConcurrency.Should().Be(4);
        subscriptionConfiguration.PrefetchCount.Should().Be(10);
    }

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        serviceProvider?.Dispose();
    }

    public async ValueTask InitializeAsync() => await autoSubscriber.SubscribeAsync([typeof(MyConsumerWithAttr)]);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // Discovered by reflection over test assembly, do not remove.
    private sealed class MyConsumerWithAttr : IConsume<MessageA>
    {
        [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
        [SubscriptionConfiguration(PrefetchCount = 10, ConsumerDispatcherConcurrency = 4)]
        public void Consume(MessageA message, CancellationToken cancellationToken)
        {
        }
    }

    private sealed class MessageA
    {
    }
}
