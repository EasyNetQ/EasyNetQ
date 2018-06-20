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
        
        private MockBuilder mockBuilder;
        byte[] body;
        private IBasicProperties properties;

        public When_publish_is_called()
        {
            mockBuilder = new MockBuilder(x => 
                x.Register<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId)));

            mockBuilder.NextModel.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null))
                .Do( x =>
                {
                    body = (byte[])x[4];
                    properties = (IBasicProperties)x[3];
                 });

            var message = new MyMessage { Text = "Hiya!" };
            mockBuilder.Bus.Publish(message);
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
            mockBuilder.Channels.Count.Should().Be(1);
        }

        [Fact]
        public void Should_call_basic_publish()
        {
            mockBuilder.Channels[0].Received().BasicPublish(
                    Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
                    Arg.Is(""),
                    Arg.Is(false),
                    Arg.Is(mockBuilder.BasicProperties), 
                    Arg.Any<byte[]>());

            var json = Encoding.UTF8.GetString(body);
            json.Should().Be("{\"Text\":\"Hiya!\"}");
        }

        [Fact]
        public void Should_put_correlationId_in_properties()
        {
            properties.CorrelationId.Should().Be(correlationId);
        }

        [Fact]
        public void Should_put_message_type_in_message_type_field()
        {
            properties.Type.Should().Be("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests");
        }

        [Fact]
        public void Should_publish_persistent_messsages()
        {
            properties.DeliveryMode.Should().Be(2);
        }

        [Fact]
        public void Should_declare_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
                Arg.Is("topic"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is<Dictionary<string, object>>( x => x.SequenceEqual(new Dictionary<string, object>())));
        }
    }

    public class When_publish_with_topic_is_called : IDisposable
    {
        private MockBuilder mockBuilder;

        public When_publish_with_topic_is_called()
        {
            mockBuilder = new MockBuilder();

            var message = new MyMessage { Text = "Hiya!" };
            mockBuilder.Bus.Publish(message, "X.A");
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
            mockBuilder.Channels[0].Received().BasicPublish(
                    Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
                    Arg.Is("X.A"),
                    Arg.Is(false),
                    Arg.Is(mockBuilder.BasicProperties),
                    Arg.Any<byte[]>());
        }
    }
}

// ReSharper restore InconsistentNaming