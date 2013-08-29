// ReSharper disable InconsistentNaming

using System;
using System.Text;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class When_Publish_is_called
    {
        private const string correlationId = "abc123";

        private MockBuilder mockBuilder;
        private IEasyNetQLogger logger;
        byte[] body;
        private IBasicProperties properties;

        [SetUp]
        public void SetUp()
        {
            logger = MockRepository.GenerateStub<IEasyNetQLogger>();
            mockBuilder = new MockBuilder(x => 
                x.Register(_ => logger)
                .Register<Func<string>>(_ => () => correlationId));

            mockBuilder.Channel.Stub(x => 
                x.BasicPublish(null, null, null, null))
                    .IgnoreArguments()
                    .Callback<string, string, IBasicProperties, byte[]>((e, r, p, b) =>
                        {
                            body = b;
                            properties = p;
                            return true;
                        });

            using (var channel = mockBuilder.Bus.OpenPublishChannel())
            {
                var message = new MyMessage { Text = "Hiya!" };
                channel.Publish(message);
            }
        }

        [Test]
        public void Should_call_basic_publish()
        {
            mockBuilder.Channel.AssertWasCalled(x => 
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
            mockBuilder.Channel.AssertWasCalled(x => x.ExchangeDeclare(
                "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests", "topic", true, false, null));
        }

        [Test]
        public void Should_close_channel()
        {
            mockBuilder.Channel.AssertWasCalled(x => x.Dispose());
        }

        [Test]
        public void Should_write_debug_message_saying_message_was_published()
        {
            logger.AssertWasCalled(x => x.DebugWrite(
                "Published to exchange: '{0}', routing key: '{1}', correlationId: '{2}'",
                "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests",
                "",
                correlationId));
        }
    }
}

// ReSharper restore InconsistentNaming