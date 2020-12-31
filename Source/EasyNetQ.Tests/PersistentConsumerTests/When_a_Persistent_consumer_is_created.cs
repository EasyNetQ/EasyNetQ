// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public class When_a_Persistent_consumer_starts_consuming : Given_a_PersistentConsumer
    {
        public When_a_Persistent_consumer_starts_consuming()
        {
            consumer.StartConsuming();
        }

        [Fact]
        public void Should_ask_the_internal_consumer_to_start_consuming()
        {
            internalConsumers[0].Received().StartConsuming();
        }

        [Fact]
        public void Should_create_internal_consumer()
        {
            internalConsumerFactory.Received(1).CreateConsumer(Arg.Any<ConsumerConfiguration>());
        }
    }
}

// ReSharper restore InconsistentNaming
