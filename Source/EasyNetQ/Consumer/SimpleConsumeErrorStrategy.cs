namespace EasyNetQ.Consumer;

/// <summary>
///     A simple strategy which does nothing, only applies AckStrategies
/// </summary>
public sealed class SimpleConsumeErrorStrategy : IConsumeErrorStrategy
{
    /// <summary>
    ///     Acks a message in case of an error
    /// </summary>
    public static readonly SimpleConsumeErrorStrategy Ack = new(AckStrategies.Ack);

    /// <summary>
    ///     Nacks a message with requeue in case of an error
    /// </summary>
    public static readonly SimpleConsumeErrorStrategy NackWithRequeue = new(AckStrategies.NackWithRequeue);

    /// <summary>
    ///     Nacks a message without requeue in case of an error
    /// </summary>
    public static readonly SimpleConsumeErrorStrategy NackWithoutRequeue = new(AckStrategies.NackWithoutRequeue);

    private readonly AckStrategyAsync errorStrategy;

    private SimpleConsumeErrorStrategy(AckStrategyAsync errorStrategy) => this.errorStrategy = errorStrategy;

    /// <inheritdoc />
    public ValueTask<AckStrategyAsync> HandleErrorAsync(
        ConsumeContext context,
        Exception exception,
        CancellationToken cancellationToken = default) => new(errorStrategy);

    /// <inheritdoc />
    public ValueTask<AckStrategyAsync> HandleCancelledAsync(
        ConsumeContext context,
        CancellationToken
        cancellationToken = default) => new(AckStrategies.NackWithRequeue);
}
