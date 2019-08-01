using System;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class PublishConfirmationListenerTest
    {
        public PublishConfirmationListenerTest()
        {
            eventBus = new EventBus();
            model = Substitute.For<IModel, IRecoverable>();
            publishConfirmationListener = new PublishConfirmationListener(eventBus);
        }

        private readonly EventBus eventBus;
        private readonly PublishConfirmationListener publishConfirmationListener;
        private readonly IModel model;
        private readonly ulong DeliveryTag = 42;

        [Fact]
        public async Task Should_fail_with_multiple_nack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag - 1, DeliveryTag);
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(model);
            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Nack(model, DeliveryTag, true));
            await Assert.ThrowsAsync<PublishNackedException>(
                () => publishConfirmationWaiter1.WaitAsync(TimeSpan.FromMilliseconds(10))
            ).ConfigureAwait(false);
            await Assert.ThrowsAsync<PublishNackedException>(
                () => publishConfirmationWaiter2.WaitAsync(TimeSpan.FromMilliseconds(10))
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_fail_with_nack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Nack(model, DeliveryTag, false));
            await Assert.ThrowsAsync<PublishNackedException>(
                () => publishConfirmationWaiter.WaitAsync(TimeSpan.FromMilliseconds(10))
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_success_with_ack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, false));
            await publishConfirmationWaiter.WaitAsync(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_success_with_multiple_ack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag - 1, DeliveryTag);
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(model);
            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, true));
            await publishConfirmationWaiter1.WaitAsync(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
            await publishConfirmationWaiter2.WaitAsync(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_timeout_without_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var publishConfirmationWaiter = publishConfirmationListener.GetWaiter(model);
            await Assert.ThrowsAsync<TimeoutException>(
                () => publishConfirmationWaiter.WaitAsync(TimeSpan.FromMilliseconds(10))
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_work_after_reconnection()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var publishConfirmationWaiter1 = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(new PublishChannelCreatedEvent(model));
            await Assert.ThrowsAsync<PublishInterruptedException>(
                () => publishConfirmationWaiter1.WaitAsync(TimeSpan.FromMilliseconds(50))
            ).ConfigureAwait(false);

            var publishConfirmationWaiter2 = publishConfirmationListener.GetWaiter(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, false));
            await publishConfirmationWaiter2.WaitAsync(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
        }
    }
}
