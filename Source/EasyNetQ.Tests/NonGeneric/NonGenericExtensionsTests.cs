using RabbitMQ.Client.Framing;
// ReSharper disable InconsistentNaming
using System;
using System.Text;
using System.Threading;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using EasyNetQ.NonGeneric;

namespace EasyNetQ.Tests.NonGeneric
{
    [TestFixture]
    public class NonGenericExtensionsTests
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
        }

        [Test]
        public void Should_be_able_to_subscribe_using_non_generic_extensions()
        {
            var are = new AutoResetEvent(false);
            MyMessage deliveredMessage = null;

            Action<object> onMessage = message =>
                {
                    deliveredMessage = (MyMessage)message;
                    are.Set();
                };

            mockBuilder.Bus.Subscribe(typeof (MyMessage), "subid", onMessage);

            var properties = new BasicProperties
                {
                    Type = "EasyNetQ.Tests.MyMessage:EasyNetQ.Tests"
                };

            var body = Encoding.UTF8.GetBytes("{ Text:\"Hello World\" }");

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer_tag",
                0,
                false,
                "exchange",
                "routing_key",
                properties,
                body);

            are.WaitOne(1000);
            deliveredMessage.ShouldNotBeNull();
            deliveredMessage.Text.ShouldEqual("Hello World");
        }
    }
}

// ReSharper restore InconsistentNaming