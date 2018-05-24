// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.HandlerRunnerTests
{
    public class When_a_user_handler_is_executed
    {
        private IHandlerRunner handlerRunner;

        byte[] deliveredBody = null;
        MessageProperties deliveredProperties = null;
        MessageReceivedInfo deliveredInfo = null;

        readonly MessageProperties messageProperties = new MessageProperties
            {
                CorrelationId = "correlation_id"
            };
        readonly MessageReceivedInfo messageInfo = new MessageReceivedInfo("consumer_tag", 123, false, "exchange", "routingKey", "queue");
        readonly byte[] messageBody = new byte[0];

        private IModel channel;

        public When_a_user_handler_is_executed()
        {
            var consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
            var eventBus = new EventBus();

            handlerRunner = new HandlerRunner(consumerErrorStrategy, eventBus);

            Func<byte[], MessageProperties, MessageReceivedInfo, Task> userHandler = (body, properties, info) => 
                Task.Factory.StartNew(() =>
                    {
                        deliveredBody = body;
                        deliveredProperties = properties;
                        deliveredInfo = info;
                    });

            var consumer = Substitute.For<IBasicConsumer>();
            channel = Substitute.For<IModel>();
            consumer.Model.Returns(channel);

            var context = new ConsumerExecutionContext(
                userHandler, messageInfo, messageProperties, messageBody, consumer);

            var autoResetEvent = new AutoResetEvent(false);
            eventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());

            handlerRunner.InvokeUserMessageHandler(context);

            autoResetEvent.WaitOne(1000);
        }

        [Fact]
        public void Should_deliver_body()
        {
            deliveredBody.ShouldBeTheSameAs(messageBody);
        }

        [Fact]
        public void Should_deliver_properties()
        {
            deliveredProperties.ShouldBeTheSameAs(messageProperties);
        }

        [Fact]
        public void Should_deliver_info()
        {
            deliveredInfo.ShouldBeTheSameAs(messageInfo);
        }

        [Fact]
        public void Should_ACK_message()
        {
            channel.Received().BasicAck(123, false);
        }
    }
}

// ReSharper restore InconsistentNaming