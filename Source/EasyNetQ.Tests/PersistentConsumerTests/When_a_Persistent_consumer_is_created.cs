// ReSharper disable InconsistentNaming

using FluentAssertions;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public class When_a_Persistent_consumer_starts_consuming : Given_a_PersistentConsumer
    {
        public override void AdditionalSetup()
        {
            persistentConnection.IsConnected.Returns(true);
            consumer.StartConsuming();
        }

        [Fact]
        public void Should_create_internal_consumer()
        {
            internalConsumerFactory.Received().CreateConsumer();
            createConsumerCalled.Should().Be(1);
        }

        [Fact]
        public void Should_ask_the_internal_consumer_to_start_consuming()
        {
            internalConsumers[0].Received().StartConsuming(persistentConnection, queue, onMessage, configuration);
        }
    }
}

// ReSharper restore InconsistentNaming