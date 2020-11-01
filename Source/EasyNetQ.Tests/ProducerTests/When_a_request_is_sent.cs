// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_request_is_sent : IDisposable
    {
        public When_a_request_is_sent()
        {
            var correlationId = Guid.NewGuid().ToString();
            mockBuilder = new MockBuilder(
                c => c.Register<ICorrelationIdGenerationStrategy>(
                    _ => new StaticCorrelationIdGenerationStrategy(correlationId)
                )
            );

            var waiter = new CountdownEvent(2);

            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(_ => waiter.Signal());
            mockBuilder.EventBus.Subscribe<StartConsumingSucceededEvent>(_ => waiter.Signal());

            var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
            if (!waiter.Wait(5000))
                throw new TimeoutException();

            DeliverMessage(correlationId);

            responseMessage = task.GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private readonly MockBuilder mockBuilder;
        private readonly TestResponseMessage responseMessage;

        private void DeliverMessage(string correlationId)
        {
            var properties = new BasicProperties
            {
                Type = "EasyNetQ.Tests.TestResponseMessage, EasyNetQ.Tests",
                CorrelationId = correlationId
            };
            var body = Encoding.UTF8.GetBytes("{ Id:12, Text:\"Hello World\"}");

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer_tag",
                0,
                false,
                "the_exchange",
                "the_routing_key",
                properties,
                body
            );
        }

        [Fact]
        public void Should_declare_the_publish_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("easy_net_q_rpc"),
                Arg.Is("direct"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Any<IDictionary<string, object>>()
            );
        }

        [Fact]
        public void Should_declare_the_response_queue()
        {
            mockBuilder.Channels[0].Received().QueueDeclare(
                Arg.Is<string>(arg => arg.StartsWith("easynetq.response.")),
                Arg.Is(false),
                Arg.Is(true),
                Arg.Is(true),
                Arg.Any<IDictionary<string, object>>()
            );
        }

        [Fact]
        public void Should_publish_request_message()
        {
            mockBuilder.Channels[2].Received().BasicPublish(
                Arg.Is("easy_net_q_rpc"),
                Arg.Is("EasyNetQ.Tests.TestRequestMessage, EasyNetQ.Tests"),
                Arg.Is(false),
                Arg.Any<IBasicProperties>(),
                Arg.Any<ReadOnlyMemory<byte>>()
            );
        }

        [Fact]
        public void Should_return_the_response()
        {
            responseMessage.Text.Should().Be("Hello World");
        }
    }
}

// ReSharper restore InconsistentNaming
