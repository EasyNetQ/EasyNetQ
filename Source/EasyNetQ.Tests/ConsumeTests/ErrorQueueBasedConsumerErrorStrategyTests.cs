using System;
using System.Text;
using EasyNetQ.Consumer;
using NSubstitute;
using RabbitMQ.Client;
using System.Threading.Tasks;
using EasyNetQ.Logging;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests;

public class ErrorQueueBasedConsumerErrorStrategyTests
{
    [Fact]
    public async Task Should_enable_publisher_confirm_when_configured_and_return_ack_when_confirm_received()
    {
        var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var modelMock = Substitute.For<IModel>();
        modelMock.WaitForConfirms(Arg.Any<TimeSpan>()).Returns(true);
        persistedConnectionMock.CreateModel().Returns(modelMock);
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock, true);

        var ackStrategy = await consumerErrorStrategy.HandleConsumerErrorAsync(CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!"), default);

        Assert.Equal(AckStrategies.Ack, ackStrategy);
        modelMock.Received().WaitForConfirms(Arg.Any<TimeSpan>());
        modelMock.Received().ConfirmSelect();
    }

    [Fact]
    public async Task
        Should_enable_publisher_confirm_when_configured_and_return_nack_with_requeue_when_no_confirm_received()
    {
        var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var modelMock = Substitute.For<IModel>();
        modelMock.WaitForConfirms(Arg.Any<TimeSpan>()).Returns(false);
        persistedConnectionMock.CreateModel().Returns(modelMock);
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock, true);

        var ackStrategy = await consumerErrorStrategy.HandleConsumerErrorAsync(CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!"), default);

        Assert.Equal(AckStrategies.NackWithRequeue, ackStrategy);
        modelMock.Received().WaitForConfirms(Arg.Any<TimeSpan>());
        modelMock.Received().ConfirmSelect();
    }

    [Fact]
    public async Task Should_not_enable_publisher_confirm_when_not_configured_and_return_ack_when_no_confirm_received()
    {
        var persistedConnectionMock = Substitute.For<IConsumerConnection>();
        var modelMock = Substitute.For<IModel>();
        modelMock.WaitForConfirms(Arg.Any<TimeSpan>()).Returns(false);
        persistedConnectionMock.CreateModel().Returns(modelMock);
        var consumerErrorStrategy = CreateConsumerErrorStrategy(persistedConnectionMock);

        var ackStrategy = await consumerErrorStrategy.HandleConsumerErrorAsync(CreateConsumerExecutionContext(CreateOriginalMessage()), new Exception("I just threw!"), default);

        Assert.Equal(AckStrategies.Ack, ackStrategy);
        modelMock.DidNotReceive().WaitForConfirms(Arg.Any<TimeSpan>());
    }

    private static ErrorQueueConsumerErrorStrategy CreateConsumerErrorStrategy(
        IConsumerConnection connectionMock, bool configurePublisherConfirm = false
    )
    {
        var consumerErrorStrategy = new ErrorQueueConsumerErrorStrategy(
            Substitute.For<ILogger<ErrorQueueConsumerErrorStrategy>>(),
            connectionMock,
            Substitute.For<ISerializer>(),
            Substitute.For<IErrorQueueConsumerErrorStrategyConventions>(),
            Substitute.For<ITypeNameSerializer>(),
            Substitute.For<IErrorMessageSerializer>(),
            new ConnectionConfiguration { PublisherConfirms = configurePublisherConfirm }
        );
        return consumerErrorStrategy;
    }

    private static ConsumerExecutionContext CreateConsumerExecutionContext(byte[] originalMessageBody)
    {
        return new ConsumerExecutionContext(
            (_, _, _, _) => null,
            new MessageReceivedInfo("consumertag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
            new MessageProperties
            {
                CorrelationId = "123",
                AppId = "456"
            },
            originalMessageBody
        );
    }

    private static byte[] CreateOriginalMessage()
    {
        const string originalMessage = "{ Text:\"Hello World\"}";
        var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);
        return originalMessageBody;
    }
}
