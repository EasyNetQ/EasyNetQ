// ReSharper disable InconsistentNaming

using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public class When_a_Persistent_consumer_starts_consuming : Given_a_PersistentConsumer
    {
        protected override void AdditionalSetup()
        {
            consumer.StartConsuming();
        }

        [Fact]
        public void Should_ask_the_internal_consumer_to_start_consuming()
        {
            internalConsumers[0].Received().StartConsuming(queue, onMessage, configuration);
        }

        [Fact]
        public void Should_create_internal_consumer()
        {
            internalConsumerFactory.Received().CreateConsumer();
            createConsumerCalled.Should().Be(1);
        }
    }
}

// ReSharper restore InconsistentNaming
