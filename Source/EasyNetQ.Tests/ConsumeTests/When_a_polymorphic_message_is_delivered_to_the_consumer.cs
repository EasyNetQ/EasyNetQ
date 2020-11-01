// ReSharper disable InconsistentNaming
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using FluentAssertions;
using RabbitMQ.Client.Framing;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_polymorphic_message_is_delivered_to_the_consumer : IDisposable
    {
        private MockBuilder mockBuilder;
        private ITestMessageInterface receivedMessage;

        public When_a_polymorphic_message_is_delivered_to_the_consumer()
        {
            mockBuilder = new MockBuilder();

            var queue = new Queue("test_queue", false);

            var are = new AutoResetEvent(false);
            mockBuilder.Bus.Advanced.Consume<ITestMessageInterface>(queue, (message, info) => Task.Factory.StartNew(() =>
                {
                    receivedMessage = message.Body;
                    are.Set();
                }));

            var publishedMessage = new Implementation { Text = "Hello Polymorphs!" };
            var body = new JsonSerializer().MessageToBytes(typeof(Implementation), publishedMessage);
            var properties = new BasicProperties
                {
                    Type = new DefaultTypeNameSerializer().Serialize(typeof(Implementation))
                };

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer_tag",
                0,
                false,
                "exchange",
                "routing_key",
                properties,
                body
                );

            if (!are.WaitOne(5000))
            {
                throw new TimeoutException();
            }
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_correctly_deserialize_message()
        {
            receivedMessage.Should().NotBeNull();
            receivedMessage.Should().BeOfType<Implementation>();
            receivedMessage.Text.Should().Be("Hello Polymorphs!");
        }
    }

    public interface ITestMessageInterface
    {
        string Text { get; set; }
    }

    public class Implementation : ITestMessageInterface
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming
