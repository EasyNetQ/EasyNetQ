namespace EasyNetQ.Consumer;

public interface IConsumeErrorStrategy
{
    /// <summary>
    /// This method is fired when an exception is thrown. Implement a strategy for
    /// handling the exception here.
    /// </summary>
    /// <param name="context">The consumer execution context.</param>
    /// <param name="exception">The exception</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="AckStrategyAsync"/> for processing the original failed message</returns>
    ValueTask<AckStrategyAsync> HandleErrorAsync(ConsumeContext context, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// This method is fired when the task returned from the UserHandler is cancelled.
    /// Implement a strategy for handling the cancellation here.
    /// </summary>
    /// <param name="context">The consumer execution context.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="AckStrategyAsync"/> for processing the original cancelled message</returns>
    ValueTask<AckStrategyAsync> HandleCancelledAsync(ConsumeContext context, CancellationToken cancellationToken = default);
}
