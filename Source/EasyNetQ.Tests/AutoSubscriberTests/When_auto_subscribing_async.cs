using EasyNetQ.AutoSubscribe;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.AutoSubscriberTests;

#pragma warning disable IDISP006
public class When_auto_subscribing_async : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;
    private readonly ServiceProvider serviceProvider;

    private const string expectedQueueName1 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_async+MessageA, EasyNetQ.Tests_my_app:a0ebdb4503cc0df2295d8d8d99c1faf8";

    private const string expectedQueueName2 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_async+MessageB, EasyNetQ.Tests_MyExplicitId";

    private const string expectedQueueName3 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_async+MessageC, EasyNetQ.Tests_my_app:930ab443eb64f3ebffd65c9e494933d9";

    public When_auto_subscribing_async()
    {
        mockBuilder = new MockBuilder();

        var services = new ServiceCollection();
        serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        var autoSubscriber = new AutoSubscriber(mockBuilder.Bus, serviceProvider, "my_app");
#pragma warning disable IDISP004
        await autoSubscriber.SubscribeAsync([typeof(MyAsyncConsumer)]);
#pragma warning restore IDISP004
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
        await serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task Should_have_declared_the_queues()
    {
        async Task VerifyQueueDeclared(string queueName) =>
            await mockBuilder.Channels[1].Received().QueueDeclareAsync(
                Arg.Is(queueName),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is(false),
                Arg.Is((IDictionary<string, object>)null),
                Arg.Is(false),
                Arg.Is(false),
                Arg.Any<CancellationToken>()
            );

        await VerifyQueueDeclared(expectedQueueName1);
        await VerifyQueueDeclared(expectedQueueName2);
        await VerifyQueueDeclared(expectedQueueName3);
    }

    [Fact]
    public async Task Should_have_bound_to_queues()
    {
        async Task ConsumerStarted(int channelIndex, string queueName, string topicName) =>
            await mockBuilder.Channels[channelIndex].Received().QueueBindAsync(
                Arg.Is(queueName),
                Arg.Any<string>(),
                Arg.Is(topicName),
                Arg.Is((IDictionary<string, object>)null),
                default,
                Arg.Any<CancellationToken>()
            );

        await ConsumerStarted(1, expectedQueueName1, "#");
        await ConsumerStarted(1, expectedQueueName2, "#");
        await ConsumerStarted(1, expectedQueueName3, "Important");
    }

    [Fact]
    public void Should_have_started_consuming_from_the_correct_queues()
    {
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName1).Should().BeTrue();
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName2).Should().BeTrue();
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName3).Should().BeTrue();
    }

    //Discovered by reflection over test assembly, do not remove.
    private sealed class MyAsyncConsumer : IConsumeAsync<MessageA>, IConsumeAsync<MessageB>, IConsumeAsync<MessageC>
    {
        public Task ConsumeAsync(MessageA message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        [AutoSubscriberConsumer(SubscriptionId = "MyExplicitId")]
        public Task ConsumeAsync(MessageB message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        [ForTopic("Important")]
        public Task ConsumeAsync(MessageC message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class MessageA
    {
    }

    private sealed class MessageB
    {
    }

    private sealed class MessageC
    {
    }
}
