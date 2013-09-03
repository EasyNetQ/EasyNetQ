// ReSharper disable InconsistentNaming

using System;
using System.Text;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
                x.Register<Func<string>>(_ => () => correlationId));

            using (var channel = mockBuilder.Bus.OpenPublishChannel())
            {
                mockBuilder.Channels[0].Stub(x =>
                    x.BasicPublish(null, null, null, null))
                        .IgnoreArguments()
                        .Callback<string, string, IBasicProperties, byte[]>((e, r, p, b) =>
                        {
                            body = b;
                            properties = p;
                            return true;
                        });

                var message = new MyMessage { Text = "Hiya!" };
                channel.Publish(message);
            }
        }

        [Test]
        public void Should_create_a_channel_to_publish_on()
        {
            // a channel is also created then disposed to declare the exchange.
            mockBuilder.Channels.Count.ShouldEqual(2);
        }

        [Test]
        public void Should_call_basic_publish()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.BasicPublish(
                    Arg<string>.Is.Equal("EasyNetQ_Tests_MyMessage:EasyNetQ_Tests"), 
                    Arg<string>.Is.Equal(""), 
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
            properties.Type.ShouldEqual("EasyNetQ_Tests_MyMessage:EasyNetQ_Tests");
        }

        [Test]
        public void Should_publish_persistent_messsages()
        {
            properties.DeliveryMode.ShouldEqual(2);
        }

        [Test]
        public void Should_declare_exchange()
        {
            mockBuilder.Channels[1].AssertWasCalled(x => x.ExchangeDeclare(
                "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests", "topic", true, false, null));
        }

        [Test]
        public void Should_close_channel()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_write_debug_message_saying_message_was_published()
        {
            mockBuilder.Logger.AssertWasCalled(x => x.DebugWrite(
                "Published to exchange: '{0}', routing key: '{1}', correlationId: '{2}'",
                "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests",
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

            using (var channel = mockBuilder.Bus.OpenPublishChannel())
            {
                var message = new MyMessage { Text = "Hiya!" };
                channel.Publish(message, x => x.WithTopic("X.A"));
            }
        }

        [Test]
        public void Should_call_basic_publish_with_correct_routing_key()
        {
            mockBuilder.Channels[0].AssertWasCalled(x =>
                x.BasicPublish(
                    Arg<string>.Is.Equal("EasyNetQ_Tests_MyMessage:EasyNetQ_Tests"),
                    Arg<string>.Is.Equal("X.A"),
                    Arg<IBasicProperties>.Is.Equal(mockBuilder.BasicProperties),
                    Arg<byte[]>.Is.Anything));
        }
    }

    [TestFixture]
    public class When_publish_with_confirms_is_called
    {
        private MockBuilder mockBuilder;
        private bool success;
        private bool failure;

        [SetUp]
        public void SetUp()
        {
            success = false;
            failure = false;
            mockBuilder = new MockBuilder();

            Action successAction = () =>
                {
                    success = true;
                };
            Action failureAction = () =>
                {
                    failure = true;
                };

            using (var channel = mockBuilder.Bus.OpenPublishChannel(x => x.WithPublisherConfirms()))
            {
                mockBuilder.Channels[0].AssertWasCalled(x => x.BasicAcks += Arg<BasicAckEventHandler>.Is.Anything);
                mockBuilder.Channels[0].AssertWasCalled(x => x.BasicNacks += Arg<BasicNackEventHandler>.Is.Anything);

                var message = new MyMessage { Text = "Hiya!" };
                channel.Publish(message, x => x
                    .OnSuccess(successAction)
                    .OnFailure(failureAction));
            }

        }

        [Test]
        public void Should_call_success_callback_when_acked()
        {
            mockBuilder.Channels[0].Raise(x => x.BasicAcks += null, mockBuilder.Channels[0], new BasicAckEventArgs());
            success.ShouldBeTrue();
            failure.ShouldBeFalse();
        }

        [Test]
        public void Should_call_failure_callback_when_nacked()
        {
            mockBuilder.Channels[0].Raise(x => x.BasicNacks += null, mockBuilder.Channels[0], new BasicNackEventArgs());
            success.ShouldBeFalse();
            failure.ShouldBeTrue();
        }
    }
}

// ReSharper restore InconsistentNaming