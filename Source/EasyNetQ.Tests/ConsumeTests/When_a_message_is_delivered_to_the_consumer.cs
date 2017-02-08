// ReSharper disable InconsistentNaming
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_message_is_delivered_to_the_consumer : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            StartConsumer((body, properties, info) => { });
            DeliverMessage();
        }

        [Fact]
        public void Should_invoke_consumer()
        {
            ConsumerWasInvoked.ShouldBeTrue();
        }

        [Fact]
        public void Should_deliver_the_message_body()
        {
            DeliveredMessageBody.ShouldBeTheSameAs(OriginalBody);
        }

        [Fact]
        public void Should_deliver_the_message_properties()
        {
            DeliveredMessageProperties.Type.ShouldBeTheSameAs(OriginalProperties.Type);
        }

        [Fact]
        public void Should_deliver_the_consumer_tag()
        {
            DeliveredMessageInfo.ConsumerTag.ShouldEqual(ConsumerTag);
        }

        [Fact]
        public void Should_deliver_the_delivery_tag()
        {
            DeliveredMessageInfo.DeliverTag.ShouldEqual(DeliverTag);
        }

        [Fact]
        public void Should_deliver_the_exchange_name()
        {
            DeliveredMessageInfo.Exchange.ShouldEqual("the_exchange");
        }

        [Fact]
        public void Should_deliver_the_routing_key()
        {
            DeliveredMessageInfo.RoutingKey.ShouldEqual("the_routing_key");
        }

        [Fact]
        public void Should_deliver_redelivered_flag()
        {
            DeliveredMessageInfo.Redelivered.ShouldBeFalse();
        }

        [Fact]
        public void Should_ack_the_message()
        {
            MockBuilder.Channels[0].Received().BasicAck(DeliverTag, false);
        }

        [Fact]
        public void Should_write_debug_message()
        {
            MockBuilder.Logger.Received().DebugWrite("Received \n\tRoutingKey: '{0}'\n\tCorrelationId: '{1}'\n\tConsumerTag: '{2}'" +
                    "\n\tDeliveryTag: {3}\n\tRedelivered: {4}",
                            "the_routing_key",
                            "the_correlation_id",
                            ConsumerTag,
                            DeliverTag,
                            false);
        }
    }
}
// ReSharper restore InconsistentNaming
