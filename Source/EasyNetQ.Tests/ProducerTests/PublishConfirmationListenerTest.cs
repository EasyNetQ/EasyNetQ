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
        public void Should_timeout_without_confirmation_event()
        {
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(DeliveryTag);
            Assert.Throws<TimeoutException>(() =>
            {
                publishConfirmationWaiter.Wait(TimeSpan.FromMilliseconds(10));
            });
        }

        [Test]
        public void Should_success_with_ack_confirmation_event()
        {
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Ack(DeliveryTag, false));
            publishConfirmationWaiter.Wait(TimeSpan.FromMilliseconds(10));
        }

        [Test]
        public void Should_fail_with_nack_confirmation_event()
        {
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Nack(DeliveryTag, false));
            Assert.Throws<PublishNackedException>(() => publishConfirmationWaiter.Wait(TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public void Should_success_with_multiple_ack_confirmation_event()
        {
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(DeliveryTag - 1);
            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Ack(DeliveryTag, true));
            publishConfirmationWaiter1.Wait(TimeSpan.FromMilliseconds(10));
            publishConfirmationWaiter2.Wait(TimeSpan.FromMilliseconds(10));
        }

        [Test]
        public void Should_fail_with_multiple_nack_confirmation_event()
        {
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(DeliveryTag - 1);
            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Nack(DeliveryTag, true));
            Assert.Throws<PublishNackedException>(() => publishConfirmationWaiter1.Wait(TimeSpan.FromMilliseconds(10)));
            Assert.Throws<PublishNackedException>(() => publishConfirmationWaiter2.Wait(TimeSpan.FromMilliseconds(10)));
        }

        [Test]
        public void Should_fail_if_get_waiter_for_deliveryTag_twice()
        {
            publishConfirmationListener.GetWaiter(DeliveryTag);
            Assert.Throws<ArgumentException>(() => publishConfirmationListener.GetWaiter(DeliveryTag));
        }

        [Test]
        public void Should_work_after_reconnection()
        {
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(DeliveryTag);
            eventBus.Publish(new PublishChannelCreatedEvent(null));
            Assert.Throws<PublishInterruptedException>(() => publishConfirmationWaiter1.Wait(TimeSpan.FromMilliseconds(50)));

            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(DeliveryTag);
            eventBus.Publish(MessageConfirmationEvent.Ack(DeliveryTag, false));
            publishConfirmationWaiter2.Wait(TimeSpan.FromMilliseconds(50));
        }

        [Test]
        public void Should_get_waiter_for_deliveryTag_again_if_previous_was_cancelled()
        {
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(DeliveryTag);
            publishConfirmationWaiter1.Cancel();
            publishConfirmationListener.GetWaiter(DeliveryTag);
        }
    }
}