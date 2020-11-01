// ReSharper disable InconsistentNaming

using EasyNetQ.Events;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public class When_the_connection_is_broken : Given_a_PersistentConsumer
    {
        protected override void AdditionalSetup()
        {
            consumer.StartConsuming();
            eventBus.Publish(new ConnectionRecoveredEvent(new AmqpTcpEndpoint()));
        }

        [Fact]
        public void Should_re_create_internal_consumer()
        {
            internalConsumerFactory.Received().CreateConsumer();
            createConsumerCalled.Should().Be(1);
            internalConsumers.Count.Should().Be(1);
            internalConsumers[0].Received(2).StartConsuming(queue, onMessage, configuration);
        }
    }
}
