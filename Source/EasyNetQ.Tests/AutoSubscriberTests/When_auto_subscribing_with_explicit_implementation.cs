using EasyNetQ.AutoSubscribe;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_with_explicit_implementation : IDisposable
{
    private readonly MockBuilder mockBuilder;
    private readonly ServiceProvider serviceProvider;

    private const string expectedQueueName1 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_with_explicit_implementation+MessageA, EasyNetQ.Tests_my_app:552bba04667af93e428cfdc296acb6d4";

    private const string expectedQueueName2 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_with_explicit_implementation+MessageB, EasyNetQ.Tests_MyExplicitId";

    private const string expectedQueueName3 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_with_explicit_implementation+MessageC, EasyNetQ.Tests_my_app:21bdba7506b28154ea638ccc10bcea2d";

    public When_auto_subscribing_with_explicit_implementation()
    {
        mockBuilder = new MockBuilder();

        var services = new ServiceCollection();
        serviceProvider = services.BuildServiceProvider();

        var autoSubscriber = new AutoSubscriber(mockBuilder.Bus, serviceProvider, "my_app");
        autoSubscriber.Subscribe(new[] { typeof(MyConsumer), typeof(MyGenericAbstractConsumer<>) });
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public void Should_have_declared_the_queues()
    {
        Action<string> assertQueueDeclared = queueName =>
            mockBuilder.Channels[1].Received().QueueDeclare(
                Arg.Is(queueName),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is(false),
                Arg.Is((IDictionary<string, object>)null)
            );

        assertQueueDeclared(expectedQueueName1);
        assertQueueDeclared(expectedQueueName2);
        assertQueueDeclared(expectedQueueName3);
    }

    [Fact]
    public void Should_have_bound_to_queues()
    {
        Action<int, string, string> assertConsumerStarted =
            (_, queueName, topicName) => mockBuilder.Channels[1].Received().QueueBind(
                Arg.Is(queueName),
                Arg.Any<string>(),
                Arg.Is(topicName),
                Arg.Is((IDictionary<string, object>)null)
            );

        assertConsumerStarted(1, expectedQueueName1, "#");
        assertConsumerStarted(2, expectedQueueName2, "#");
        assertConsumerStarted(3, expectedQueueName3, "Important");
    }

    [Fact]
    public void Should_have_started_consuming_from_the_correct_queues()
    {
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName1).Should().BeTrue();
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName2).Should().BeTrue();
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName3).Should().BeTrue();
    }

    // Discovered by reflection over test assembly, do not remove.
    private sealed class MyConsumer : IConsume<MessageA>, IConsume<MessageB>, IConsume<MessageC>
    {
        void IConsume<MessageA>.Consume(MessageA message, CancellationToken cancellationToken)
        {
        }

        [AutoSubscriberConsumer(SubscriptionId = "MyExplicitId")]
        void IConsume<MessageB>.Consume(MessageB message, CancellationToken cancellationToken)
        {
        }

        [ForTopic("Important")]
        void IConsume<MessageC>.Consume(MessageC message, CancellationToken cancellationToken)
        {
        }
    }

    //Discovered by reflection over test assembly, do not remove.
    private abstract class MyGenericAbstractConsumer<TMessage> : IConsume<TMessage>
        where TMessage : class
    {
        public virtual void Consume(TMessage message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class MessageA
    {
        public string Text { get; set; }
    }

    private sealed class MessageB
    {
        public string Text { get; set; }
    }

    private sealed class MessageC
    {
        public string Text { get; set; }
    }
}
