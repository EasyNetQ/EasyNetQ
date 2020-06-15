// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using EasyNetQ.Producer;
using EasyNetQ.Tests.Mocking;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_message_is_sent : IDisposable
    {
        private readonly MockBuilder mockBuilder;
        private const string queueName = "the_queue_name";

        public When_a_message_is_sent()
        {
            mockBuilder = new MockBuilder();

            mockBuilder.SendReceive.Send(queueName, new MyMessage { Text = "Hello World" });
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
                Arg.Any<ReadOnlyMemory<byte>>()
            );
        }
    }
}

// ReSharper restore InconsistentNaming
