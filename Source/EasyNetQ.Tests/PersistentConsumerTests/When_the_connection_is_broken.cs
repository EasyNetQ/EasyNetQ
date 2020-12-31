// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using EasyNetQ.Events;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public class When_the_connection_is_broken : Given_a_PersistentConsumer
    {
        public When_the_connection_is_broken()
        {
            consumer.StartConsuming();
            eventBus.Publish(new ConnectionRecoveredEvent(new AmqpTcpEndpoint()));
        }

        [Fact]
        public void Should_re_create_internal_consumer()
        {
            internalConsumerFactory.Received(1).CreateConsumer(Arg.Any<ConsumerConfiguration>());
            internalConsumers.Count.Should().Be(1);
            internalConsumers[0].Received(2).StartConsuming(Arg.Any<bool>());
        }
    }
}
