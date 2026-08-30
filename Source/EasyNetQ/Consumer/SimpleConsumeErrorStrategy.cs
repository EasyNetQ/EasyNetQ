using EasyNetQ.Pipeline;

namespace EasyNetQ.Consumer;

/// <summary>
///     A simple strategy which does nothing but return a fixed <see cref="AckDecision" />
/// </summary>
public sealed class SimpleConsumeErrorStrategy : IConsumeErrorStrategy
{
    /// <summary>
    ///     Acks a message in case of an error
    /// </summary>
    public static readonly SimpleConsumeErrorStrategy Ack = new(AckDecision.Ack);

    /// <summary>
    ///     Nacks a message with requeue in case of an error
    /// </summary>
    public static readonly SimpleConsumeErrorStrategy NackWithRequeue = new(AckDecision.NackRequeue);

    /// <summary>
    ///     Nacks a message without requeue in case of an error
    /// </summary>
    public static readonly SimpleConsumeErrorStrategy NackWithoutRequeue = new(AckDecision.NackDiscard);

    private readonly AckDecision errorDecision;

    private SimpleConsumeErrorStrategy(AckDecision errorDecision) => this.errorDecision = errorDecision;

    /// <inheritdoc />
    public ValueTask<AckDecision> HandleErrorAsync(
        ConsumeContext context,
        Exception exception,
        CancellationToken cancellationToken = default
    ) => new(errorDecision);

    /// <inheritdoc />
    public ValueTask<AckDecision> HandleCancelledAsync(
        ConsumeContext context,
        CancellationToken cancellationToken = default
    ) => new(AckDecision.NackRequeue);
}
