// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using NSubstitute;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;
using EasyNetQ.Logging;
using Xunit;

namespace EasyNetQ.Tests.HandlerRunnerTests;

public class When_a_user_handler_is_cancelled
{
    private readonly MessageProperties messageProperties = new()
    {
        CorrelationId = "correlation_id"
    };
    private readonly MessageReceivedInfo messageInfo = new("consumer_tag", 42, false, "exchange", "routingKey", "queue");
    private readonly byte[] messageBody = Array.Empty<byte>();

    private readonly IConsumerErrorStrategy consumerErrorStrategy;
    private readonly ConsumerExecutionContext context;
    private readonly IModel channel;

    public When_a_user_handler_is_cancelled()
    {
        consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
        consumerErrorStrategy.HandleConsumerCancelledAsync(default).ReturnsForAnyArgs(Task.FromResult(AckStrategies.Ack));

        var handlerRunner = new HandlerRunner(Substitute.For<ILogger<IHandlerRunner>>(), consumerErrorStrategy);

        var consumer = Substitute.For<IBasicConsumer>();
        channel = Substitute.For<IModel>();
        consumer.Model.Returns(channel);

        context = new ConsumerExecutionContext(
            (_, _, _, _) => Task.FromException<AckStrategy>(new OperationCanceledException()),
            messageInfo,
            messageProperties,
            messageBody
        );

        var handlerTask = handlerRunner.InvokeUserMessageHandlerAsync(context, default)
            .ContinueWith(async x =>
            {
                var ackStrategy = await x;
                return ackStrategy(channel, 42);
            }, TaskContinuationOptions.ExecuteSynchronously)
            .Unwrap();

        if (!handlerTask.Wait(5000))
        {
            throw new TimeoutException();
        }
    }

    [Fact]
    public async Task Should_handle_consumer_cancelled()
    {
        await consumerErrorStrategy.Received().HandleConsumerCancelledAsync(context);
    }

    [Fact]
    public void Should_Ack()
    {
        channel.Received().BasicAck(42, false);
    }
}

// ReSharper restore InconsistentNaming
