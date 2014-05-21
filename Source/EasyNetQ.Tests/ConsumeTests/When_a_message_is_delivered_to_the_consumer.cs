// ReSharper disable InconsistentNaming
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_a_message_is_delivered_to_the_consumer : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            StartConsumer((body, properties, info) => { });
            DeliverMessage();
        }

        [Test]
        public void Should_invoke_consumer()
        {
            ConsumerWasInvoked.ShouldBeTrue();
        }

        [Test]
        public void Should_deliver_the_message_body()
        {
            DeliveredMessageBody.ShouldBeTheSameAs(OriginalBody);
        }

        [Test]
        public void Should_deliver_the_message_properties()
        {
            DeliveredMessageProperties.Type.ShouldBeTheSameAs(OriginalProperties.Type);
        }

        [Test]
        public void Should_deliver_the_consumer_tag()
        {
            DeliveredMessageInfo.ConsumerTag.ShouldEqual(ConsumerTag);
        }

        [Test]
        public void Should_deliver_the_delivery_tag()
        {
            DeliveredMessageInfo.DeliverTag.ShouldEqual(DeliverTag);
        }

        [Test]
        public void Should_deliver_the_exchange_name()
        {
            DeliveredMessageInfo.Exchange.ShouldEqual("the_exchange");
        }

        [Test]
        public void Should_deliver_the_routing_key()
        {
            DeliveredMessageInfo.RoutingKey.ShouldEqual("the_routing_key");
        }

        [Test]
        public void Should_deliver_redelivered_flag()
        {
            DeliveredMessageInfo.Redelivered.ShouldBeFalse();
        }

        [Test]
        public void Should_ack_the_message()
        {
            MockBuilder.Channels[0].AssertWasCalled(x => x.BasicAck(DeliverTag, false));
        }

        [Test]
        public void Should_write_debug_message()
        {
            MockBuilder.Logger.AssertWasCalled(x =>
                x.DebugWrite("Received \n\tRoutingKey: '{0}'\n\tCorrelationId: '{1}'\n\tConsumerTag: '{2}'" +
                    "\n\tDeliveryTag: {3}\n\tRedelivered: {4}",
                            "the_routing_key",
                            "the_correlation_id",
                            ConsumerTag,
                            DeliverTag,
                            false));
        }
    }
}
// ReSharper restore InconsistentNaming
