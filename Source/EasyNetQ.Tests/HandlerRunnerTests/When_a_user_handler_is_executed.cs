// ReSharper disable InconsistentNaming

using System;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.HandlerRunnerTests
{
    public class When_a_user_handler_is_executed
    {
        public When_a_user_handler_is_executed()
        {
            var consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();

            var handlerRunner = new HandlerRunner(consumerErrorStrategy);

            var consumer = Substitute.For<IBasicConsumer>();
            channel = Substitute.For<IModel, IRecoverable>();
            consumer.Model.Returns(channel);

            var context = new ConsumerExecutionContext(
                async (body, properties, info, cancellation) =>
                {
                    deliveredBody = body;
                    deliveredProperties = properties;
                    deliveredInfo = info;
                    return AckStrategies.Ack;
                },
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

        private byte[] deliveredBody;
        private MessageProperties deliveredProperties;
        private MessageReceivedInfo deliveredInfo;

        private readonly MessageProperties messageProperties = new MessageProperties
        {
            CorrelationId = "correlation_id"
        };

        private readonly MessageReceivedInfo messageInfo = new MessageReceivedInfo("consumer_tag", 42, false, "exchange", "routingKey", "queue");
        private readonly byte[] messageBody = new byte[0];

        private readonly IModel channel;

        [Fact]
        public void Should_ACK()
        {
            channel.Received().BasicAck(42, false);
        }

        [Fact]
        public void Should_deliver_body()
        {
            deliveredBody.Should().BeSameAs(messageBody);
        }

        [Fact]
        public void Should_deliver_info()
        {
            deliveredInfo.Should().BeSameAs(messageInfo);
        }

        [Fact]
        public void Should_deliver_properties()
        {
            deliveredProperties.Should().BeSameAs(messageProperties);
        }
    }
}

// ReSharper restore InconsistentNaming
