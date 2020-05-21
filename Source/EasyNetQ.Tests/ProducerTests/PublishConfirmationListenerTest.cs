﻿using System;
using System.Threading;
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
        private const ulong DeliveryTag = 42;

        [Fact]
        public async Task Should_fail_with_multiple_nack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag - 1, DeliveryTag);
            var confirmation1 = publishConfirmationListener.CreatePendingConfirmation(model);
            var confirmation2 = publishConfirmationListener.CreatePendingConfirmation(model);
            eventBus.Publish(MessageConfirmationEvent.Nack(model, DeliveryTag, true));
            await Assert.ThrowsAsync<PublishNackedException>(
                () => confirmation1.WaitAsync(default)
            ).ConfigureAwait(false);
            await Assert.ThrowsAsync<PublishNackedException>(
                () => confirmation2.WaitAsync(default)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_fail_with_nack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var confirmation = publishConfirmationListener.CreatePendingConfirmation(model);
            eventBus.Publish(MessageConfirmationEvent.Nack(model, DeliveryTag, false));
            await Assert.ThrowsAsync<PublishNackedException>(
                () => confirmation.WaitAsync(default)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_success_with_ack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var confirmation = publishConfirmationListener.CreatePendingConfirmation(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, false));
            await confirmation.WaitAsync(default).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_success_with_multiple_ack_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag - 1, DeliveryTag);
            var confirmation1 = publishConfirmationListener.CreatePendingConfirmation(model);
            var confirmation2 = publishConfirmationListener.CreatePendingConfirmation(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, true));
            await confirmation1.WaitAsync(default).ConfigureAwait(false);
            await confirmation2.WaitAsync(default).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_cancel_without_confirmation_event()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var confirmation = publishConfirmationListener.CreatePendingConfirmation(model);
            using var cts = new CancellationTokenSource(1000);
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => confirmation.WaitAsync(cts.Token)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_work_after_reconnection()
        {
            model.NextPublishSeqNo.Returns(DeliveryTag);
            var confirmation1 = publishConfirmationListener.CreatePendingConfirmation(model);
            eventBus.Publish(new PublishChannelCreatedEvent(model));
            await Assert.ThrowsAsync<PublishInterruptedException>(
                () => confirmation1.WaitAsync(default)
            ).ConfigureAwait(false);

            var confirmation2 = publishConfirmationListener.CreatePendingConfirmation(model);
            eventBus.Publish(MessageConfirmationEvent.Ack(model, DeliveryTag, false));
            await confirmation2.WaitAsync(default).ConfigureAwait(false);
        }
    }
}
