using System.Text;
using EasyNetQ.Consumer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public class DefaultConsumerErrorStrategyTests
{
    [Fact]
    public async Task Should_enable_publisher_confirm_when_configured_and_return_ack_when_confirm_received()
    {
        var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var modelMock = Substitute.For<IChannel>();
        using var cts = new CancellationTokenSource(Arg.Any<TimeSpan>());
        modelMock.WaitForConfirmsAsync(cts.Token).Returns(true);
        persistedConnectionMock.CreateChannelAsync().Returns(modelMock);
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock, true);

        var ackStrategy = await consumerErrorStrategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!")
        );

        Assert.Equal(AckStrategies.Ack, ackStrategy);
        await modelMock.Received().WaitForConfirmsAsync(cts.Token);
        await modelMock.Received().ConfirmSelectAsync();
    }

    [Fact]
    public async Task
        Should_enable_publisher_confirm_when_configured_and_return_nack_with_requeue_when_no_confirm_received()
    {
        var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var modelMock = Substitute.For<IChannel>();
        using var cts = new CancellationTokenSource(Arg.Any<TimeSpan>());
        modelMock.WaitForConfirmsAsync(cts.Token).Returns(false);
        persistedConnectionMock.CreateChannelAsync().Returns(modelMock);
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock, true);

        var ackStrategy = await consumerErrorStrategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!")
        );

        Assert.Equal(AckStrategies.NackWithRequeue, ackStrategy);
        await modelMock.Received().WaitForConfirmsAsync(cts.Token);
        await modelMock.Received().ConfirmSelectAsync();
    }

    [Fact]
    public async Task Should_not_enable_publisher_confirm_when_not_configured_and_return_ack_when_no_confirm_received()
    {
        var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var modelMock = Substitute.For<IChannel>();
        using var cts = new CancellationTokenSource(Arg.Any<TimeSpan>());
        modelMock.WaitForConfirmsAsync(cts.Token).Returns(false);
        persistedConnectionMock.CreateChannelAsync().Returns(modelMock);
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock);

        var ackStrategy = await consumerErrorStrategy.HandleErrorAsync(
            CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!")
        );

        Assert.Equal(AckStrategies.Ack, ackStrategy);
        await modelMock.DidNotReceive().WaitForConfirmsAsync(cts.Token);
    }

    private static DefaultConsumeErrorStrategy CreateConsumerErrorStrategy(
        IConsumerConnection connectionMock, bool configurePublisherConfirm = false
    )
    {
        var consumerErrorStrategy = new DefaultConsumeErrorStrategy(
            Substitute.For<ILogger<DefaultConsumeErrorStrategy>>(),
            connectionMock,
            Substitute.For<ISerializer>(),
            Substitute.For<IConventions>(),
            Substitute.For<ITypeNameSerializer>(),
            Substitute.For<IErrorMessageSerializer>(),
            new ConnectionConfiguration { PublisherConfirms = configurePublisherConfirm }
        );
        return consumerErrorStrategy;
    }

    private static ConsumeContext CreateConsumerExecutionContext(byte[] originalMessageBody)
    {
        return new ConsumeContext(
            new MessageReceivedInfo("consumertag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
            new MessageProperties
            {
                CorrelationId = "123",
                AppId = "456"
            },
            originalMessageBody,
            Substitute.For<IServiceProvider>(),
            CancellationToken.None
        );
    }

    private static byte[] CreateOriginalMessage()
    {
        const string originalMessage = "{ Text:\"Hello World\"}";
        var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);
        return originalMessageBody;
    }
}
