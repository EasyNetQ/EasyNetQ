using System;
// ReSharper disable InconsistentNaming
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client.Framing;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_message_without_typeinfo_is_delivered_to_the_consumer : IDisposable
    {
        private MockBuilder mockBuilder;
        ITestMessageInterface receivedMessage;

        public When_a_message_without_typeinfo_is_delivered_to_the_consumer()
        {
            mockBuilder = new MockBuilder();

            var queue = new Queue("test_queue", false);

            var are = new AutoResetEvent(false);
            mockBuilder.Bus.Advanced.Consume<Implementation>(queue, (message, info) => Task.Factory.StartNew(() =>
            {
                receivedMessage = message.Body;
                are.Set();
            }));

            var publishedMessage = new Implementation { Text = "Hello Polymorphs!" };
            var body = new JsonSerializer(new TypeNameSerializer()).MessageToBytes(publishedMessage);
            var properties = new BasicProperties
            {
                //Type = new TypeNameSerializer().Serialize(typeof(Implementation))
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

            are.WaitOne(1000);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_correctly_deserialize_message()
        {
            receivedMessage.ShouldNotBeNull();
            receivedMessage.GetType().ShouldEqual(typeof(Implementation));
            receivedMessage.Text.ShouldEqual("Hello Polymorphs!");
        }
    }

}

// ReSharper restore InconsistentNaming