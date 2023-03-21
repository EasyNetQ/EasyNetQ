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

    private readonly AckStrategy errorStrategy;

    private SimpleConsumeErrorStrategy(AckStrategy errorStrategy) => this.errorStrategy = errorStrategy;

    /// <inheritdoc />
    public ValueTask<AckStrategy> HandleErrorAsync(ConsumeContext context, Exception exception) => new(errorStrategy);

    /// <inheritdoc />
    public ValueTask<AckStrategy> HandleCancelledAsync(ConsumeContext context) => new(AckStrategies.NackWithRequeue);
}
