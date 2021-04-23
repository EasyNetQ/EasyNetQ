// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using NSubstitute;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.Tests.HandlerRunnerTests
{
    public class When_a_user_handler_is_failed
    {
        private readonly MessageProperties messageProperties = new MessageProperties
        {
            CorrelationId = "correlation_id"
        };
        private readonly MessageReceivedInfo messageInfo = new MessageReceivedInfo("consumer_tag", 123, false, "exchange", "routingKey", "queue");
        private readonly byte[] messageBody = new byte[0];

        private readonly IConsumerErrorStrategy consumerErrorStrategy;
        private readonly ConsumerExecutionContext context;
        private readonly IModel channel;

        public When_a_user_handler_is_failed()
        {
            consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
            consumerErrorStrategy.HandleConsumerErrorAsync(default, null).ReturnsForAnyArgs(Task.FromResult(AckStrategies.Ack));

            var handlerRunner = new HandlerRunner(consumerErrorStrategy);

            var consumer = Substitute.For<IBasicConsumer>();
            channel = Substitute.For<IModel>();
            consumer.Model.Returns(channel);

            context = new ConsumerExecutionContext(
                (body, properties, info, cancellation) => Task.FromException<AckStrategy>(new Exception()),
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
        public async Task Should_handle_consumer_error()
        {
            await consumerErrorStrategy.Received().HandleConsumerErrorAsync(context, Arg.Any<Exception>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void Should_ACK()
        {
            channel.Received().BasicAck(42, false);
        }
    }
}

// ReSharper restore InconsistentNaming
