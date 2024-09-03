using System;
using System.Text;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests;

public class PublishConfirmationListenerTest : IDisposable
{
    private bool disposed;

    public PublishConfirmationListenerTest()
    {
        eventBus = new EventBus(Substitute.For<ILogger<EventBus>>());
        channel = Substitute.For<IChannel, IRecoverable>();
        publishConfirmationListener = new PublishConfirmationListener(eventBus);
    }

    private readonly EventBus eventBus;
    private readonly PublishConfirmationListener publishConfirmationListener;
    private readonly IChannel channel;
    private const ulong DeliveryTag = 42;

    [Fact]
    public async Task Should_fail_with_multiple_nack_confirmation_event()
    {
        channel.NextPublishSeqNo.Returns(DeliveryTag - 1, DeliveryTag);
        var confirmation1 = publishConfirmationListener.CreatePendingConfirmation(channel);
        var confirmation2 = publishConfirmationListener.CreatePendingConfirmation(channel);
        eventBus.Publish(MessageConfirmationEvent.Nack(channel, DeliveryTag, true));
        await Assert.ThrowsAsync<PublishNackedException>(
            () => confirmation1.WaitAsync()
        );
        await Assert.ThrowsAsync<PublishNackedException>(
            () => confirmation2.WaitAsync()
        );
    }

    [Fact]
    public async Task Should_fail_with_nack_confirmation_event()
    {
        channel.NextPublishSeqNo.Returns(DeliveryTag);
        var confirmation = publishConfirmationListener.CreatePendingConfirmation(channel);
        eventBus.Publish(MessageConfirmationEvent.Nack(channel, DeliveryTag, false));
        await Assert.ThrowsAsync<PublishNackedException>(
            () => confirmation.WaitAsync()
        );
    }

    [Fact]
    public async Task Should_success_with_ack_confirmation_event()
    {
        channel.NextPublishSeqNo.Returns(DeliveryTag);
        var confirmation = publishConfirmationListener.CreatePendingConfirmation(channel);
        eventBus.Publish(MessageConfirmationEvent.Ack(channel, DeliveryTag, false));
        await confirmation.WaitAsync();
    }

    [Fact]
    public async Task Should_success_with_multiple_ack_confirmation_event()
    {
        channel.NextPublishSeqNo.Returns(DeliveryTag - 1, DeliveryTag);
        var confirmation1 = publishConfirmationListener.CreatePendingConfirmation(channel);
        var confirmation2 = publishConfirmationListener.CreatePendingConfirmation(channel);
        eventBus.Publish(MessageConfirmationEvent.Ack(channel, DeliveryTag, true));
        await confirmation1.WaitAsync();
        await confirmation2.WaitAsync();
    }

    [Fact]
    public async Task Should_cancel_without_confirmation_event()
    {
        channel.NextPublishSeqNo.Returns(DeliveryTag);
        var confirmation = publishConfirmationListener.CreatePendingConfirmation(channel);
        using var cts = new CancellationTokenSource(1000);
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => confirmation.WaitAsync(cts.Token)
        );
    }

    [Fact]
    public async Task Should_work_after_reconnection()
    {
        channel.NextPublishSeqNo.Returns(DeliveryTag);
        var confirmation1 = publishConfirmationListener.CreatePendingConfirmation(channel);
        eventBus.Publish(new ChannelRecoveredEvent(channel));
        await Assert.ThrowsAsync<PublishInterruptedException>(
            () => confirmation1.WaitAsync()
        );

        var confirmation2 = publishConfirmationListener.CreatePendingConfirmation(channel);
        eventBus.Publish(MessageConfirmationEvent.Ack(channel, DeliveryTag, false));
        await confirmation2.WaitAsync();
    }

    [Fact]
    public async Task Should_fail_with_returned_message_event()
    {
        channel.NextPublishSeqNo.Returns(DeliveryTag);
        var confirmation1 = publishConfirmationListener.CreatePendingConfirmation(channel);
        var properties = new MessageProperties
        {
            Headers = new Dictionary<string, object>
            {
                { MessagePropertiesExtensions.ConfirmationIdHeader, Encoding.UTF8.GetBytes(confirmation1.Id.ToString()) }
            }
        };
        eventBus.Publish(
            new ReturnedMessageEvent(
                channel,
                Array.Empty<byte>(),
                properties,
                new MessageReturnedInfo("exchange", "routingKey", "returnReason")
            )
        );
        await Assert.ThrowsAsync<PublishReturnedException>(
            () => confirmation1.WaitAsync()
        );
    }

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        publishConfirmationListener.Dispose();
    }
}
