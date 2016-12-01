using System;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.ProducerTests
{
    public class PublishConfirmationListenerTest
    {
        private EventBus eventBus;
        private PublishConfirmationListener publishConfirmationListener;
        private IModel model;
        private ulong DeliveryTag = 42;

        public PublishConfirmationListenerTest()
        {
            eventBus = new EventBus();
            model = Substitute.For<IModel>();
            publishConfirmationListener = new PublishConfirmationListener(eventBus);
        }

        [Fact]
        public void Should_timeout_without_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(model);
            Assert.Throws<TimeoutException>(() =>
            {
                publishConfirmationWaiter.Wait(TimeSpan.FromMilliseconds(10));
            });
        }

        [Fact]
        public void Should_success_with_ack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, false));
            publishConfirmationWaiter.Wait(TimeSpan.FromMilliseconds(10));
        }

        [Fact]
        public void Should_fail_with_nack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Nack(model, DeliveryTag, false));
            Assert.Throws<PublishNackedException>(() => publishConfirmationWaiter.Wait(TimeSpan.FromMilliseconds(10)));
        }

        [Fact]
        public void Should_success_with_multiple_ack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag - 1, DeliveryTag);
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(model);
            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, true));
            publishConfirmationWaiter1.Wait(TimeSpan.FromMilliseconds(10));
            publishConfirmationWaiter2.Wait(TimeSpan.FromMilliseconds(10));
        }

        [Fact]
        public void Should_fail_with_multiple_nack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag - 1, DeliveryTag);
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(model);
            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Nack(model, DeliveryTag,  true));
            Assert.Throws<PublishNackedException>(() => publishConfirmationWaiter1.Wait(TimeSpan.FromMilliseconds(10)));
            Assert.Throws<PublishNackedException>(() => publishConfirmationWaiter2.Wait(TimeSpan.FromMilliseconds(10)));
        }

        [Fact]
        public void Should_work_after_reconnection()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(new PublishChannelCreatedEvent(model));
            Assert.Throws<PublishInterruptedException>(() => publishConfirmationWaiter1.Wait(TimeSpan.FromMilliseconds(50)));

            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, false));
            publishConfirmationWaiter2.Wait(TimeSpan.FromMilliseconds(50));
        }
    }
}