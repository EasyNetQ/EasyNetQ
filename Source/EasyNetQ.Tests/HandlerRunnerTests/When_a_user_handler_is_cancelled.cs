// ReSharper disable InconsistentNaming

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using FluentAssertions;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.HandlerRunnerTests
{
    public class When_a_user_handler_is_cancelled
    {
        private readonly MessageProperties messageProperties = new MessageProperties
            {
                CorrelationId = "correlation_id"
            };
        private readonly MessageReceivedInfo messageInfo = new MessageReceivedInfo("consumer_tag", 123, false, "exchange", "routingKey", "queue");
        private readonly byte[] messageBody = new byte[0];

        private readonly IConsumerErrorStrategy consumerErrorStrategy;
        private readonly ConsumerExecutionContext context;

        public When_a_user_handler_is_cancelled()
        {
            consumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
            var eventBus = new EventBus();

            var handlerRunner = new HandlerRunner(consumerErrorStrategy, eventBus);

            Func<byte[], MessageProperties, MessageReceivedInfo, Task> userHandler = (body, properties, info) => 
                Task.Run(() => throw new OperationCanceledException());

            var consumer = Substitute.For<IBasicConsumer>();
            var channel = Substitute.For<IModel>();
            consumer.Model.Returns(channel);

            context = new ConsumerExecutionContext(userHandler, messageInfo, messageProperties, messageBody, consumer);

            var autoResetEvent = new AutoResetEvent(false);
            eventBus.Subscribe<AckEvent>(x => autoResetEvent.Set());

            handlerRunner.InvokeUserMessageHandler(context);

            autoResetEvent.WaitOne(1000);
        }

        [Fact]
        public void Should_handle_consumer_cancelled()
        {
            consumerErrorStrategy.Received().HandleConsumerCancelled(context);
        }
    }
}

// ReSharper restore InconsistentNaming