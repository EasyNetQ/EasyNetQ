// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class When_publish_is_called
    {
        private const string correlationId = "abc123";
        
        private MockBuilder mockBuilder;
        byte[] body;
        private IBasicProperties properties;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder(x => 
                x.Register<ICorrelationIdGenerationStrategy>(_ => new StaticCorrelationIdGenerationStrategy(correlationId)));

            mockBuilder.NextModel.Stub(x =>
                x.BasicPublish(null, null, false, false, null, null))
                    .IgnoreArguments()
                    .Callback<string, string, bool, bool, IBasicProperties, byte[]>((e, r, m, i, p, b) =>
                    {
                        body = b;
                        properties = p;
                        return true;
                    });

            var message = new MyMessage { Text = "Hiya!" };
            mockBuilder.Bus.Publish(message);
            WaitForMessageToPublish();
        }

        private void WaitForMessageToPublish()
        {
            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(x => autoResetEvent.Set());
            autoResetEvent.WaitOne(1000);
        }

        [Test]
        public void Should_create_a_channel_to_publish_on()
        {
            // a channel is also created then disposed to declare the exchange.
            mockBuilder.Channels.Count.ShouldEqual(1);
        }

        [Test]
        public void Should_call_basic_publish()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.BasicPublish(
                    Arg<string>.Is.Equal("EasyNetQ.Tests.MyMessage:EasyNetQ.Tests"), 
                    Arg<string>.Is.Equal(""), 
                    Arg<bool>.Is.Equal(false),
                    Arg<bool>.Is.Equal(false),
                    Arg<IBasicProperties>.Is.Equal(mockBuilder.BasicProperties), 
                    Arg<byte[]>.Is.Anything));

            var json = Encoding.UTF8.GetString(body);
            json.ShouldEqual("{\"Text\":\"Hiya!\"}");
        }

        [Test]
        public void Should_put_correlationId_in_properties()
        {
            properties.CorrelationId.ShouldEqual(correlationId);
        }

        [Test]
        public void Should_put_message_type_in_message_type_field()
        {
            properties.Type.ShouldEqual("EasyNetQ.Tests.MyMessage:EasyNetQ.Tests");
        }

        [Test]
        public void Should_publish_persistent_messsages()
        {
            properties.DeliveryMode.ShouldEqual(2);
        }

        [Test]
        public void Should_declare_exchange()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.ExchangeDeclare(
                "EasyNetQ.Tests.MyMessage:EasyNetQ.Tests", "topic", true, false, new Dictionary<string, object>()));
        }

        [Test]
        public void Should_write_debug_message_saying_message_was_published()
        {
            mockBuilder.Logger.AssertWasCalled(x => x.DebugWrite(
                "Published to exchange: '{0}', routing key: '{1}', correlationId: '{2}'",
                "EasyNetQ.Tests.MyMessage:EasyNetQ.Tests",
                "",
                correlationId));
        }
    }

    [TestFixture]
    public class When_publish_with_topic_is_called
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            var message = new MyMessage { Text = "Hiya!" };
            mockBuilder.Bus.Publish(message, "X.A");
            WaitForMessageToPublish();
        }

        private void WaitForMessageToPublish()
        {
            var autoResetEvent = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(x => autoResetEvent.Set());
            autoResetEvent.WaitOne(1000);
        }

        [Test]
        public void Should_call_basic_publish_with_correct_routing_key()
        {
            mockBuilder.Channels[0].AssertWasCalled(x =>
                x.BasicPublish(
                    Arg<string>.Is.Equal("EasyNetQ.Tests.MyMessage:EasyNetQ.Tests"),
                    Arg<string>.Is.Equal("X.A"),
                    Arg<bool>.Is.Equal(false),
                    Arg<bool>.Is.Equal(false),
                    Arg<IBasicProperties>.Is.Equal(mockBuilder.BasicProperties),
                    Arg<byte[]>.Is.Anything));
        }
    }
}

// ReSharper restore InconsistentNaming