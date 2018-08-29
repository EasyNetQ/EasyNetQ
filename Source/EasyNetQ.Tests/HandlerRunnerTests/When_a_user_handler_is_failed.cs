﻿// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using FluentAssertions;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

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
            consumerErrorStrategy.HandleConsumerError(null, null).ReturnsForAnyArgs(AckStrategies.Ack);

            var handlerRunner = new HandlerRunner(consumerErrorStrategy);

            var consumer = Substitute.For<IBasicConsumer>();
            channel = Substitute.For<IModel>();
            consumer.Model.Returns(channel);

            context = new ConsumerExecutionContext(
                (body, properties, info, cancellation) => TaskHelpers.FromException(new Exception()),
                messageInfo,
                messageProperties,
                messageBody
            );

            var handlerTask = handlerRunner.InvokeUserMessageHandlerAsync(context, default)
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
        public void Should_handle_consumer_error()
        {
            consumerErrorStrategy.Received().HandleConsumerError(context, Arg.Any<Exception>());
        }

        [Fact]
        public void Should_ACK()
        {
            channel.Received().BasicAck(42, false);
        }
    }
}

// ReSharper restore InconsistentNaming