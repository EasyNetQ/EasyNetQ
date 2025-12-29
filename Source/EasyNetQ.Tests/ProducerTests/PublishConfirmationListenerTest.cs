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
        using var cts = new CancellationTokenSource();
        channel.GetNextPublishSequenceNumberAsync(cts.Token).Returns(DeliveryTag - 1, DeliveryTag);
        var confirmation1 = await publishConfirmationListener.CreatePendingConfirmationAsync(channel, cts.Token);
        var confirmation2 = await publishConfirmationListener.CreatePendingConfirmationAsync(channel, cts.Token);
        await eventBus.PublishAsync(MessageConfirmationEvent.Nack(channel, DeliveryTag, true));
        await Assert.ThrowsAsync<PublishNackedException>(
            () => confirmation1.WaitAsync(cts.Token)
        );
        await Assert.ThrowsAsync<PublishNackedException>(
            () => confirmation2.WaitAsync(cts.Token)
        );
    }

    [Fact]
    public async Task Should_fail_with_nack_confirmation_event()
    {
        channel.GetNextPublishSequenceNumberAsync().Returns(DeliveryTag);
        var confirmation = await publishConfirmationListener.CreatePendingConfirmationAsync(channel);
        await eventBus.PublishAsync(MessageConfirmationEvent.Nack(channel, DeliveryTag, false));
        await Assert.ThrowsAsync<PublishNackedException>(
            () => confirmation.WaitAsync(CancellationToken.None)
        );
    }

    [Fact]
    public async Task Should_success_with_ack_confirmation_event()
    {
        channel.GetNextPublishSequenceNumberAsync().Returns(DeliveryTag);
        var confirmation = await publishConfirmationListener.CreatePendingConfirmationAsync(channel);
        await eventBus.PublishAsync(MessageConfirmationEvent.Ack(channel, DeliveryTag, false));
        await confirmation.WaitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Should_success_with_multiple_ack_confirmation_event()
    {
        channel.GetNextPublishSequenceNumberAsync().Returns(DeliveryTag - 1, DeliveryTag);
        var confirmation1 = await publishConfirmationListener.CreatePendingConfirmationAsync(channel);
        var confirmation2 = await publishConfirmationListener.CreatePendingConfirmationAsync(channel);
        await eventBus.PublishAsync(MessageConfirmationEvent.Ack(channel, DeliveryTag, true));
        await confirmation1.WaitAsync(CancellationToken.None);
        await confirmation2.WaitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Should_cancel_without_confirmation_event()
    {
        channel.GetNextPublishSequenceNumberAsync().Returns(DeliveryTag);
        var confirmation = await publishConfirmationListener.CreatePendingConfirmationAsync(channel);
        using var cts = new CancellationTokenSource(1000);
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => confirmation.WaitAsync(cts.Token)
        );
    }

    [Fact]
    public async Task Should_work_after_reconnection()
    {
        channel.GetNextPublishSequenceNumberAsync().Returns(DeliveryTag);
        var confirmation1 = await publishConfirmationListener.CreatePendingConfirmationAsync(channel);
        await eventBus.PublishAsync(new ChannelRecoveredEvent(channel));
        await Assert.ThrowsAsync<PublishInterruptedException>(
            () => confirmation1.WaitAsync(CancellationToken.None)
        );

        var confirmation2 = await publishConfirmationListener.CreatePendingConfirmationAsync(channel);
        await eventBus.PublishAsync(MessageConfirmationEvent.Ack(channel, DeliveryTag, false));
        await confirmation2.WaitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Should_fail_with_returned_message_event()
    {
        channel.GetNextPublishSequenceNumberAsync().Returns(DeliveryTag);
        var confirmation1 = await publishConfirmationListener.CreatePendingConfirmationAsync(channel);
        var properties = new MessageProperties
        {
            Headers = new Dictionary<string, object>
            {
                { MessagePropertiesExtensions.ConfirmationIdHeader, Encoding.UTF8.GetBytes(confirmation1.Id.ToString()) }
            }
        };
        await eventBus.PublishAsync(
            new ReturnedMessageEvent(
                channel,
                Array.Empty<byte>(),
                properties,
                new MessageReturnedInfo("exchange", "routingKey", "returnReason")
            )
        );
        await Assert.ThrowsAsync<PublishReturnedException>(
            () => confirmation1.WaitAsync(CancellationToken.None)
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
