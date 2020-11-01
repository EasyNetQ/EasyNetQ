// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests
{
    public class When_publish_is_called : IDisposable
    {
        private const string correlationId = "abc123";

        private readonly MockBuilder mockBuilder;

        public When_publish_is_called()
        {
            mockBuilder = new MockBuilder(x =>
                x.Register<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId))
            );

            var message = new MyMessage { Text = "Hiya!" };
            mockBuilder.PubSub.Publish(message);
            WaitForMessageToPublish();
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private void WaitForMessageToPublish()
        {
            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(x => autoResetEvent.Set());
            autoResetEvent.WaitOne(1000);
        }

        [Fact]
        public void Should_create_a_channel_to_publish_on()
        {
            // a channel is also created then disposed to declare the exchange.
            mockBuilder.Channels.Count.Should().Be(2);
        }

        [Fact]
        public void Should_call_basic_publish()
        {
            mockBuilder.Channels[1].Received().BasicPublish(
                Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
                Arg.Is(""),
                Arg.Is(false),
                Arg.Is<IBasicProperties>(
                    x => x.CorrelationId == correlationId
                         && x.Type == "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"
                         && x.DeliveryMode == 2
                ),
                Arg.Is<ReadOnlyMemory<byte>>(
                    x => x.ToArray().SequenceEqual(Encoding.UTF8.GetBytes("{\"Text\":\"Hiya!\"}"))
                )
            );
        }

        [Fact]
        public void Should_declare_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
                Arg.Is("topic"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is((IDictionary<string, object>)null)
            );
        }
    }

    public class When_publish_with_topic_is_called : IDisposable
    {
        private readonly MockBuilder mockBuilder;

        public When_publish_with_topic_is_called()
        {
            mockBuilder = new MockBuilder();

            var message = new MyMessage { Text = "Hiya!" };
            mockBuilder.PubSub.Publish(message, c => c.WithTopic("X.A"));
            WaitForMessageToPublish();
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private void WaitForMessageToPublish()
        {
            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(x => autoResetEvent.Set());
            autoResetEvent.WaitOne(1000);
        }

        [Fact]
        public void Should_call_basic_publish_with_correct_routing_key()
        {
            mockBuilder.Channels[1].Received().BasicPublish(
                    Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
                    Arg.Is("X.A"),
                    Arg.Is(false),
                    Arg.Any<IBasicProperties>(),
                    Arg.Any<ReadOnlyMemory<byte>>()
            );
        }
    }
}

// ReSharper restore InconsistentNaming
