// ReSharper disable InconsistentNaming

using EasyNetQ.Events;
using FluentAssertions;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public class When_the_connection_is_broken : Given_a_PersistentConsumer
    {
        public override void AdditionalSetup()
        {
            persistentConnection.IsConnected.Returns(true);
            consumer.StartConsuming();
            eventBus.Publish(new ConnectionCreatedEvent());
        }

        [Fact]
        public void Should_re_create_internal_consumer()
        {
            internalConsumerFactory.Received().CreateConsumer();
            createConsumerCalled.Should().Be(2);
            internalConsumers.Count.Should().Be(2);
        }
    }
}