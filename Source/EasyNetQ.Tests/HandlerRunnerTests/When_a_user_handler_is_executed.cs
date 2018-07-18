// ReSharper disable InconsistentNaming

using System;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using FluentAssertions;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.HandlerRunnerTests
{
    public class When_a_user_handler_is_executed
    {
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

        public When_a_user_handler_is_executed()
        {
            var consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();

            var handlerRunner = new HandlerRunner(consumerErrorStrategy);

            var consumer = Substitute.For<IBasicConsumer>();
            channel = Substitute.For<IModel>();
            consumer.Model.Returns(channel);

            var context = new ConsumerExecutionContext(
                (body, properties, info) => Task.Run(() =>
                    {
                        deliveredBody = body;
                        deliveredProperties = properties;
                        deliveredInfo = info;
                    }),
                messageInfo,
                messageProperties,
                messageBody
            );

            var handlerTask = handlerRunner.InvokeUserMessageHandlerAsync(context)
                .ContinueWith(async x =>
                {
                    var ackStrategy = await x.ConfigureAwait(false);
                    return ackStrategy(channel, 42);
                }, TaskContinuationOptions.ExecuteSynchronously)
                .Unwrap();

            if (!handlerTask.Wait(5000))
            {
                throw new TimeoutException();
            }
        }

        [Fact]
        public void Should_deliver_body()
        {
            deliveredBody.Should().BeSameAs(messageBody);
        }

        [Fact]
        public void Should_deliver_properties()
        {
            deliveredProperties.Should().BeSameAs(messageProperties);
        }

        [Fact]
        public void Should_deliver_info()
        {
            deliveredInfo.Should().BeSameAs(messageInfo);
        }

        [Fact]
        public void Should_ACK()
        {
            channel.Received().BasicAck(42, false);
        }
    }
}

// ReSharper restore InconsistentNaming