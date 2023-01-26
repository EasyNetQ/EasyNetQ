namespace EasyNetQ.Consumer;

/// <summary>
///     A simple strategy which does nothing, only applies AckStrategies
/// </summary>
public class SimpleConsumerErrorStrategy : IConsumerErrorStrategy
{
    /// <summary>
    ///     Acks a message in case of an error
    /// </summary>
    public static readonly SimpleConsumerErrorStrategy Ack = new(AckStrategies.Ack);

    /// <summary>
    ///     Nacks a message with requeue in case of an error
    /// </summary>
    public static readonly SimpleConsumerErrorStrategy NackWithRequeue = new(AckStrategies.NackWithRequeue);

    /// <summary>
    ///     Nacks a message without requeue in case of an error
    /// </summary>
    public static readonly SimpleConsumerErrorStrategy NackWithoutRequeue = new(AckStrategies.NackWithoutRequeue);

    private readonly AckStrategy errorStrategy;

    private SimpleConsumerErrorStrategy(AckStrategy errorStrategy) => this.errorStrategy = errorStrategy;

    /// <inheritdoc />
    public Task<AckStrategy> HandleConsumerErrorAsync(ConsumerExecutionContext context, Exception exception, CancellationToken cancellationToken = default) => Task.FromResult(errorStrategy);

    /// <inheritdoc />
    public Task<AckStrategy> HandleConsumerCancelledAsync(ConsumerExecutionContext context, CancellationToken cancellationToken = default) => Task.FromResult(AckStrategies.NackWithRequeue);
}
