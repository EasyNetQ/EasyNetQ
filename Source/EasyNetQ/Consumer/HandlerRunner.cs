using EasyNetQ.Events;
using EasyNetQ.Logging;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Consumer
{
    public interface IHandlerRunner : IDisposable
    {
        Task<AckStrategy> InvokeUserMessageHandlerAsync(ConsumerExecutionContext context, CancellationToken cancellationToken = default);
    }

    public class HandlerRunner : IHandlerRunner
    {
        private readonly ILog logger = LogProvider.For<HandlerRunner>();
        private readonly IConsumerErrorStrategy consumerErrorStrategy;

        public HandlerRunner(IConsumerErrorStrategy consumerErrorStrategy)
        {
            Preconditions.CheckNotNull(consumerErrorStrategy, nameof(consumerErrorStrategy));

            this.consumerErrorStrategy = consumerErrorStrategy;
        }

        /// <inheritdoc />
        public virtual async Task<AckStrategy> InvokeUserMessageHandlerAsync(
            ConsumerExecutionContext context, CancellationToken cancellationToken
        )
        {
            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Received message with receivedInfo={receivedInfo}", context.ReceivedInfo);
            }

            var ackStrategy = await InvokeUserMessageHandlerInternalAsync(context, cancellationToken).ConfigureAwait(false);

            return (model, tag) =>
            {
                try
                {
                    return ackStrategy(model, tag);
                }
                catch (AlreadyClosedException alreadyClosedException)
                {
                    logger.Info(
                        alreadyClosedException,
                        "Failed to ACK or NACK, message will be retried, receivedInfo={receivedInfo}",
                        context.ReceivedInfo
                    );
                }
                catch (IOException ioException)
                {
                    logger.Info(
                        ioException,
                        "Failed to ACK or NACK, message will be retried, receivedInfo={receivedInfo}",
                        context.ReceivedInfo
                    );
                }
                catch (Exception exception)
                {
                    logger.Error(
                        exception,
                        "Unexpected exception when attempting to ACK or NACK, receivedInfo={receivedInfo}",
                        context.ReceivedInfo
                    );
                }

                return AckResult.Exception;
            };
        }

        private async Task<AckStrategy> InvokeUserMessageHandlerInternalAsync(
            ConsumerExecutionContext context, CancellationToken cancellationToken
        )
        {
            try
            {
                try
                {
                    return await context.Handler(
                        context.Body, context.Properties, context.ReceivedInfo, cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return await consumerErrorStrategy.HandleConsumerCancelledAsync(
                        context, cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    return await consumerErrorStrategy.HandleConsumerErrorAsync(
                        context, exception, cancellationToken
                    ).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Consumer error strategy has failed");
                return AckStrategies.NackWithRequeue;
            }
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
        }
    }
}
