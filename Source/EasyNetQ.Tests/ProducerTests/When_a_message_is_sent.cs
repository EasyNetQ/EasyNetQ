// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

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

        [Test]
        public void Should_publish_the_message()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.BasicPublish(
                Arg<string>.Is.Equal(""),
                Arg<string>.Is.Equal(queueName),
                Arg<bool>.Is.Equal(false),
                Arg<bool>.Is.Equal(false),
                Arg<IBasicProperties>.Is.Anything,
                Arg<byte[]>.Is.Anything));
        }

        [Test]
        public void Should_declare_the_queue()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.QueueDeclare(
                Arg<string>.Is.Equal(queueName),
                Arg<bool>.Is.Equal(true),
                Arg<bool>.Is.Equal(false),
                Arg<bool>.Is.Equal(false),
                Arg<IDictionary<string, object>>.Is.Anything));
        }
    }
}

// ReSharper restore InconsistentNaming