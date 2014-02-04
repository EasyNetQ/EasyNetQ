using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumerErrorStrategy : IDisposable
    {
        /// <summary>
        /// This method is fired when an exception is thrown. Implement a strategy for
        /// handling the exception here.
        /// </summary>
        /// <param name="context">The consumer execution context.</param>
        /// <param name="exception">The exception</param>
        PostExceptionAckStrategy HandleConsumerError(ConsumerExecutionContext context, Exception exception);
    }

    public enum PostExceptionAckStrategy
    {
        ShouldAck,
        ShouldNackWithoutRequeue,
        ShouldNackWithRequeue,
        DoNothing
    }
}