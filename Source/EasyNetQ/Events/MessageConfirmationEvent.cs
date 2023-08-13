using RabbitMQ.Client;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a message is acked or nacked
/// </summary>
/// <param name="Channel">The channel</param>
/// <param name="DeliveryTag">The delivery tag</param>
/// <param name="Multiple"><see langword="true"/> if a confirmation affects all previous messages</param>
/// <param name="IsNack"><see langword="true"/> if a message is rejected</param>
public readonly record struct MessageConfirmationEvent(IModel Channel, ulong DeliveryTag, bool Multiple, bool IsNack)
{
    /// <summary>
    ///     Creates ack event
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="deliveryTag">The delivery tag</param>
    /// <param name="multiple">The multiple option</param>
    /// <returns></returns>
    public static MessageConfirmationEvent Ack(IModel channel, ulong deliveryTag, bool multiple) =>
        new(channel, deliveryTag, multiple, false);

    /// <summary>
    ///     Creates nack event
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="deliveryTag">The delivery tag</param>
    /// <param name="multiple">The multiple option</param>
    /// <returns></returns>
    public static MessageConfirmationEvent Nack(IModel channel, ulong deliveryTag, bool multiple) =>
        new(channel, deliveryTag, multiple, true);
}
