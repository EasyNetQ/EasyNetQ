namespace EasyNetQ.Consumer;

public interface IConsumeErrorStrategy
{
    /// <summary>
    /// This method is fired when an exception is thrown. Implement a strategy for
    /// handling the exception here.
    /// </summary>
    /// <param name="context">The consumer execution context.</param>
    /// <param name="exception">The exception</param>
    /// <returns><see cref="AckStrategy"/> for processing the original failed message</returns>
    ValueTask<AckStrategy> HandleErrorAsync(ConsumeContext context, Exception exception);

    /// <summary>
    /// This method is fired when the task returned from the UserHandler is cancelled.
    /// Implement a strategy for handling the cancellation here.
    /// </summary>
    /// <param name="context">The consumer execution context.</param>
    /// <returns><see cref="AckStrategy"/> for processing the original cancelled message</returns>
    ValueTask<AckStrategy> HandleCancelledAsync(ConsumeContext context);
}
