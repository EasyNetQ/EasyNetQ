// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_nack_received_from_the_message_handler : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            StartConsumer((body, properties, info) => AckStrategies.NackWithRequeue);
            DeliverMessage();
        }

        [Fact]
        public void Should_nack()
        {
            MockBuilder.Channels[0].Received().BasicNack(DeliverTag, false, true);
        }

        [Fact]
        public void Should_dispose_of_the_consumer_error_strategy_when_the_bus_is_disposed()
        {
            MockBuilder.Bus.Dispose();

            ConsumerErrorStrategy.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming
