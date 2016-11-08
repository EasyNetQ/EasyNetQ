// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class When_a_message_is_sent
    {
        private MockBuilder mockBuilder;
        private const string queueName = "the_queue_name";

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            mockBuilder.Bus.Send(queueName, new MyMessage { Text = "Hello World" });
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        } 

        [Test]
        public void Should_publish_the_message()
        {
            mockBuilder.Channels[0].Received().BasicPublish(
                Arg.Is(""),
                Arg.Is(queueName),
                Arg.Is(false),
                Arg.Any<IBasicProperties>(),
                Arg.Any<byte[]>());
        }

        [Test]
        public void Should_declare_the_queue()
        {
            mockBuilder.Channels[0].Received().QueueDeclare(
                Arg.Is(queueName),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is(false),
                Arg.Any<IDictionary<string, object>>());
        }
    }
}

// ReSharper restore InconsistentNaming