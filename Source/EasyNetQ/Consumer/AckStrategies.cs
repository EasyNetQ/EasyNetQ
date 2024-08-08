using EasyNetQ.Events;
using RabbitMQ.Client;

namespace EasyNetQ.Consumer;

/// <summary>
///     Represents a strategy of a message's acknowledgment
/// </summary>
public delegate Task<AckResult> AckStrategy(IChannel model, ulong deliveryTag);

/// <summary>
///     Various strategies of a message's acknowledgment
/// </summary>
public static class AckStrategies
{
    /// <summary>
    ///     Positive acknowledgment of a message
    /// </summary>
    public static readonly AckStrategy Ack = async (model, tag) =>
    {
        await model.BasicAckAsync(tag, false);
        return AckResult.Ack;
    };

    /// <summary>
    ///     Negative acknowledgment of a message without requeue
    /// </summary>
    public static readonly AckStrategy NackWithoutRequeue = async (model, tag) =>
    {
        await model.BasicNackAsync(tag, false, false);
        return AckResult.Nack;
    };

    /// <summary>
    ///     Negative acknowledgment of a message with requeue
    /// </summary>
    public static readonly AckStrategy NackWithRequeue = async (model, tag) =>
    {
        await model.BasicNackAsync(tag, false, true);
        return AckResult.Nack;
    };
}
