using System;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using NUnit.Framework;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class PublishConfirmationListenerTest
    {
        private EventBus eventBus;
        private PublishConfirmationListener publishConfirmationListener;
        private const ulong DeliveryTag = 42;

        [SetUp]
        public void SetUp()
        {
            eventBus = new EventBus();
            publishConfirmationListener = new PublishConfirmationListener(eventBus);
        }

        [Test]
        public void Should_fail_if_wait_without_request()
        {
            Assert.Throws<OperationCanceledException>(() => publishConfirmationListener.Wait(DeliveryTag, TimeSpan.FromMilliseconds(100)));
        }

        [Test]
        public void Should_timout_without_confirmation_event()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                publishConfirmationListener.Request(DeliveryTag);
                publishConfirmationListener.Wait(DeliveryTag, TimeSpan.FromMilliseconds(10));
            });
        }

        [Test]
        public void Should_success_with_ack_confirmation_event()
        {
            publishConfirmationListener.Request(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Ack(DeliveryTag, false));
            publishConfirmationListener.Wait(DeliveryTag, TimeSpan.FromMilliseconds(10));
        }

        [Test]
        public void Should_fail_with_nack_confirmation_event()
        {
            publishConfirmationListener.Request(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Nack(DeliveryTag, false));
            Assert.Throws<PublishNackedException>(() => publishConfirmationListener.Wait(DeliveryTag, TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public void Should_success_with_multiple_ack_confirmation_event()
        {
            publishConfirmationListener.Request(DeliveryTag - 1);
            publishConfirmationListener.Request(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Ack(DeliveryTag, true));
            publishConfirmationListener.Wait(DeliveryTag - 1, TimeSpan.FromMilliseconds(10));
            publishConfirmationListener.Wait(DeliveryTag, TimeSpan.FromMilliseconds(10));
        }

        [Test]
        public void Should_fail_with_multiple_nack_confirmation_event()
        {
            publishConfirmationListener.Request(DeliveryTag - 1);
            publishConfirmationListener.Request(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Nack(DeliveryTag, true));
            Assert.Throws<PublishNackedException>(() => publishConfirmationListener.Wait(DeliveryTag - 1, TimeSpan.FromMilliseconds(10)));
            Assert.Throws<PublishNackedException>(() => publishConfirmationListener.Wait(DeliveryTag, TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public void Should_fail_if_request_deliveryTag_twice()
        {
            publishConfirmationListener.Request(DeliveryTag);
            Assert.Throws<ArgumentException>(() => publishConfirmationListener.Request(DeliveryTag));
        }

        [Test]
        public void Should_work_after_reconnection()
        {
            publishConfirmationListener.Request(DeliveryTag);
            eventBus.Publish(new PublishChannelCreatedEvent(null));
            Assert.Throws<OperationCanceledException>(() => publishConfirmationListener.Wait(DeliveryTag, TimeSpan.FromMilliseconds(50)));

            publishConfirmationListener.Request(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Ack(DeliveryTag, false));
            publishConfirmationListener.Wait(DeliveryTag, TimeSpan.FromMilliseconds(50));
        }

        [Test]
        public void Should_request_deliveryTag_again_if_previous_was_cancelled()
        {
            publishConfirmationListener.Request(DeliveryTag);
            publishConfirmationListener.Cancel(DeliveryTag);
            publishConfirmationListener.Request(DeliveryTag);
        }
    }
}