// ReSharper disable InconsistentNaming
using RabbitMQ.Client.Framing;
using System;
using System.Text;
using System.Threading;
using EasyNetQ.Tests.Mocking;
using Xunit;
using EasyNetQ.NonGeneric;
using FluentAssertions;

namespace EasyNetQ.Tests.NonGeneric
{
    public class NonGenericExtensionsTests : IDisposable
    {
        private MockBuilder mockBuilder;

        public NonGenericExtensionsTests()
        {
            mockBuilder = new MockBuilder();
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
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
                    Type = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"
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
            deliveredMessage.Should().NotBeNull();
            deliveredMessage.Text.Should().Be("Hello World");
        }
    }
}

// ReSharper restore InconsistentNaming