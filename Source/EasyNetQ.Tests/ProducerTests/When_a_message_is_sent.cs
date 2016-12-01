// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using EasyNetQ.Tests.Mocking;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_message_is_sent : IDisposable
    {
        private MockBuilder mockBuilder;
        private const string queueName = "the_queue_name";

        public When_a_message_is_sent()
        {
            mockBuilder = new MockBuilder();

            mockBuilder.Bus.Send(queueName, new MyMessage { Text = "Hello World" });
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        } 

        [Fact]
        public void Should_publish_the_message()
        {
            mockBuilder.Channels[0].Received().BasicPublish(
                Arg.Is(""),
                Arg.Is(queueName),
                Arg.Is(false),
                Arg.Any<IBasicProperties>(),
                Arg.Any<byte[]>());
        }

        [Fact]
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