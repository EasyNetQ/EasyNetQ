using EasyNetQ.Pipeline;

namespace EasyNetQ.Consumer;

/// <summary>
///     Decides what happens to a message whose handling failed or was cancelled
/// </summary>
public interface IConsumeErrorStrategy
{
    /// <summary>
    ///     Called when an exception is thrown while handling a message. Implement a strategy for
    ///     handling the exception here.
    /// </summary>
    /// <param name="context">The consume context of the failed message</param>
    /// <param name="exception">The exception</param>
    /// <param name="cancellationToken">Cancelled when the consumer stops</param>
    /// <returns>The <see cref="AckDecision" /> for the original failed message</returns>
    ValueTask<AckDecision> HandleErrorAsync(ConsumeContext context, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Called when handling of a message was cancelled because the consumer is stopping.
    ///     Implement a strategy for handling the cancellation here.
    /// </summary>
    /// <param name="context">The consume context of the cancelled message</param>
    /// <param name="cancellationToken">Cancelled when the consumer stops</param>
    /// <returns>The <see cref="AckDecision" /> for the original cancelled message</returns>
    ValueTask<AckDecision> HandleCancelledAsync(ConsumeContext context, CancellationToken cancellationToken = default);
}
