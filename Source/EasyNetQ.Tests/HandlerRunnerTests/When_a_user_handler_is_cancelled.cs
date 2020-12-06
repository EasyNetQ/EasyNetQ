// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using NSubstitute;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.Tests.HandlerRunnerTests
{
    public class When_a_user_handler_is_cancelled
    {
        private readonly MessageProperties messageProperties = new MessageProperties
            {
                CorrelationId = "correlation_id"
            };
        private readonly MessageReceivedInfo messageInfo = new MessageReceivedInfo("consumer_tag", 42, false, "exchange", "routingKey", "queue");
        private readonly byte[] messageBody = new byte[0];

        private readonly IConsumerErrorStrategy consumerErrorStrategy;
        private readonly ConsumerExecutionContext context;
        private readonly IModel channel;

        public When_a_user_handler_is_cancelled()
        {
            consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
            consumerErrorStrategy.HandleConsumerCancelled(default).ReturnsForAnyArgs(AckStrategies.Ack);

            var handlerRunner = new HandlerRunner(consumerErrorStrategy);

            var consumer = Substitute.For<IBasicConsumer>();
            channel = Substitute.For<IModel>();
            consumer.Model.Returns(channel);

            context = new ConsumerExecutionContext(
                async (body, properties, info, cancellation) => throw new OperationCanceledException(),
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
        public void Should_handle_consumer_cancelled()
        {
            consumerErrorStrategy.Received().HandleConsumerCancelled(context);
        }

        [Fact]
        public void Should_Ack()
        {
            channel.Received().BasicAck(42, false);
        }
    }
}

// ReSharper restore InconsistentNaming
