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
        void InvokeUserMessageHandler(ConsumerExecutionContext context);
    }

    public class HandlerRunner : IHandlerRunner
    {
        private readonly ILog logger = LogProvider.For<HandlerRunner>();
        private readonly IConsumerErrorStrategy consumerErrorStrategy;
        private readonly IEventBus eventBus;

        public HandlerRunner(IConsumerErrorStrategy consumerErrorStrategy, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(consumerErrorStrategy, "consumerErrorStrategy");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.consumerErrorStrategy = consumerErrorStrategy;
            this.eventBus = eventBus;
        }

        public virtual void InvokeUserMessageHandler(ConsumerExecutionContext context)
        {
            Preconditions.CheckNotNull(context, "context");

            logger.DebugFormat("Received message with receivedInfo={receivedInfo}", context.Info);

            Task completionTask;
            
            try
            {
                completionTask = context.UserHandler(context.Body, context.Properties, context.Info);
            }
            catch (Exception exception)
            {
                completionTask = TaskHelpers.FromException(exception);
            }
            
            completionTask.ContinueWith(task => DoAck(context, GetAckStrategy(context, task)));
        }

        protected virtual AckStrategy GetAckStrategy(ConsumerExecutionContext context, Task task)
        {
            try
            {
                if (task.IsFaulted)
                {
                    logger.Error(
                        task.Exception,
                        "Exception thrown by subscription callback, receivedInfo={receivedInfo}, properties={properties}, message={message}", 
                        context.Info,
                        context.Properties,
                        Convert.ToBase64String(context.Body)
                    );
                    return consumerErrorStrategy.HandleConsumerError(context, task.Exception);
                }

                if (task.IsCanceled)
                {
                    return consumerErrorStrategy.HandleConsumerCancelled(context);
                }

                return AckStrategies.Ack;
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Consumer error strategy has failed");
                return AckStrategies.NackWithRequeue;
            }
        }

        protected virtual void DoAck(ConsumerExecutionContext context, AckStrategy ackStrategy)
        {
            var ackResult = AckResult.Exception;

            try
            {
                Preconditions.CheckNotNull(context.Consumer.Model, "context.Consumer.Model");

                ackResult = ackStrategy(context.Consumer.Model, context.Info.DeliverTag);
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
            finally
            {
                eventBus.Publish(new AckEvent(context.Info, context.Properties, context.Body, ackResult));
            }
        }

        public void Dispose()
        {
            consumerErrorStrategy?.Dispose();
        }
    }
}