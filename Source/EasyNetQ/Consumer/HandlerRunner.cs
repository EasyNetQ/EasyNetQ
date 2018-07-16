using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Logging;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer
{
    public interface IHandlerRunner : IDisposable
    {
        Task<AckStrategy> InvokeUserMessageHandlerAsync(ConsumerExecutionContext context);
    }

    public class HandlerRunner : IHandlerRunner
    {
        private readonly ILog logger = LogProvider.For<HandlerRunner>();
        private readonly IConsumerErrorStrategy consumerErrorStrategy;

        public HandlerRunner(IConsumerErrorStrategy consumerErrorStrategy)
        {
            Preconditions.CheckNotNull(consumerErrorStrategy, "consumerErrorStrategy");

            this.consumerErrorStrategy = consumerErrorStrategy;
        }

        public virtual async Task<AckStrategy> InvokeUserMessageHandlerAsync(ConsumerExecutionContext context)
        {
            Preconditions.CheckNotNull(context, "context");

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Received message with receivedInfo={receivedInfo}", context.Info);
            }

            var ackStrategy = await InvokeUserMessageHandlerInternalAsync(context).ConfigureAwait(false);
            
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
                        context.Info
                    );
                }
                catch (IOException ioException)
                {
                    logger.Info(
                        ioException,
                        "Failed to ACK or NACK, message will be retried, receivedInfo={receivedInfo}",
                        context.Info
                    );
                }
                catch (Exception exception)
                {
                    logger.Error(
                        exception, 
                        "Unexpected exception when attempting to ACK or NACK, receivedInfo={receivedInfo}",
                        context.Info
                    );
                }
                
                return AckResult.Exception;
            };
        }

        private async Task<AckStrategy> InvokeUserMessageHandlerInternalAsync(ConsumerExecutionContext context)
        {
            try
            {
                try
                {
                    await context.UserHandler(context.Body, context.Properties, context.Info).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return consumerErrorStrategy.HandleConsumerCancelled(context);
                }
                catch (Exception exception)
                {
                    logger.Error(
                        exception,
                        "Exception thrown by subscription callback, receivedInfo={receivedInfo}, properties={properties}, message={message}",
                        context.Info,
                        context.Properties,
                        Convert.ToBase64String(context.Body)
                    );
                    return consumerErrorStrategy.HandleConsumerError(context, exception);
                }
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Consumer error strategy has failed");
                return AckStrategies.NackWithRequeue;
            }

            return AckStrategies.Ack;
        }

        public void Dispose()
        {
            consumerErrorStrategy.Dispose();
        }
    }
}