using System;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    public interface IConsumerErrorStrategy : IDisposable
    {
        /// <summary>
        /// This method is fired when an exception is thrown. Implement a strategy for
        /// handling the exception here.
        /// </summary>
        /// <param name="devliverArgs">The AMQP delivery args</param>
        /// <param name="exception">The exception</param>
        void HandleConsumerError(BasicDeliverEventArgs devliverArgs, Exception exception);

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