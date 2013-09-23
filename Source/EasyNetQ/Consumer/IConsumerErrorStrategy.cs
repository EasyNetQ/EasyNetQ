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
        void HandleConsumerError(ConsumerExecutionContext context, Exception exception);

        /// <summary>
        /// Should the message be ack'd after HandleConsumerError has been run. Return
        /// true if it should be ack'd, false if it shouldn't
        /// </summary>
        /// <returns></returns>
        PostExceptionAckStrategy PostExceptionAckStrategy();
    }

    public enum PostExceptionAckStrategy
    {
        ShouldAck,
        ShouldNackWithoutRequeue,
        ShouldNackWithRequeue,
        DoNothing
    }
}