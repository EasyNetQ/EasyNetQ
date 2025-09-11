using System.Threading.Tasks;
using System.Threading;
using EasyNetQ.Events;
using RabbitMQ.Client;

namespace EasyNetQ.Consumer;

/// <summary>
///     Represents a strategy of a message's acknowledgment
/// </summary>
public delegate Task<AckResult> AckStrategyAsync(IChannel model, ulong deliveryTag, CancellationToken cancellationToken);

/// <summary>
///     Various strategies of a message's acknowledgment
/// </summary>
public static class AckStrategies
{
    /// <summary>
    ///     Positive acknowledgment of a message
    /// </summary>
    public static readonly AckStrategyAsync AckAsync = async (model, tag, cancellationToken) =>
    {
        await model.BasicAckAsync(tag, false, cancellationToken);
        return AckResult.Ack;
    };

    /// <summary>
    ///     Negative acknowledgment of a message without requeue
    /// </summary>
    public static readonly AckStrategyAsync NackWithoutRequeueAsync = async (model, tag, cancellationToken) =>
    {
        await model.BasicNackAsync(tag, false, false, cancellationToken);
        return AckResult.Nack;
    };

    /// <summary>
    ///     Negative acknowledgment of a message with requeue
    /// </summary>
    public static readonly AckStrategyAsync NackWithRequeueAsync = async (model, tag, cancellationToken) =>
    {
        await model.BasicNackAsync(tag, false, true, cancellationToken);
        return AckResult.Nack;
    };
}
