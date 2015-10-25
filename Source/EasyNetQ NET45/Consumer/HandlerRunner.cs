using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer
{
    public interface IHandlerRunner : IDisposable
    {
        void InvokeUserMessageHandler(ConsumerExecutionContext context);
    }

    public class HandlerRunner : IHandlerRunner
    {
        private readonly IEasyNetQLogger logger;
        private readonly IConsumerErrorStrategy consumerErrorStrategy;
        private readonly IEventBus eventBus;

        public HandlerRunner(IEasyNetQLogger logger, IConsumerErrorStrategy consumerErrorStrategy, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(consumerErrorStrategy, "consumerErrorStrategy");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.logger = logger;
            this.consumerErrorStrategy = consumerErrorStrategy;
            this.eventBus = eventBus;
        }

        public void InvokeUserMessageHandler(ConsumerExecutionContext context)
        {
            Preconditions.CheckNotNull(context, "context");

            logger.DebugWrite("Received \n\tRoutingKey: '{0}'\n\tCorrelationId: '{1}'\n\tConsumerTag: '{2}'" +
                "\n\tDeliveryTag: {3}\n\tRedelivered: {4}",
                context.Info.RoutingKey,
                context.Properties.CorrelationId,
                context.Info.ConsumerTag,
                context.Info.DeliverTag,
                context.Info.Redelivered);

            Task completionTask;
            
            try
            {
                completionTask = context.UserHandler(context.Body, context.Properties, context.Info);
            }
            catch (Exception exception)
            {
                completionTask = TaskHelpers.FromException(exception);
            }
            
            if (completionTask.Status == TaskStatus.Created)
            {
                logger.ErrorWrite("Task returned from consumer callback is not started. ConsumerTag: '{0}'",
                    context.Info.ConsumerTag);
                return;
            }
            
            completionTask.ContinueWith(task => DoAck(context, GetAckStrategy(context, task)));
        }

        private AckStrategy GetAckStrategy(ConsumerExecutionContext context, Task task)
        {
            var ackStrategy = AckStrategies.Ack;
            try
            {
                if (task.IsFaulted)
                {
                    logger.ErrorWrite(BuildErrorMessage(context, task.Exception));
                    ackStrategy = consumerErrorStrategy.HandleConsumerError(context, task.Exception);
                }
                else if (task.IsCanceled)
                {
                    ackStrategy = consumerErrorStrategy.HandleConsumerCancelled(context);
                }
            }
            catch (Exception consumerErrorStrategyError)
            {
                logger.ErrorWrite("Exception in ConsumerErrorStrategy:\n{0}",
                                  consumerErrorStrategyError);
                return AckStrategies.Nothing;
            }
            return ackStrategy;
        }
        
        private void DoAck(ConsumerExecutionContext context, AckStrategy ackStrategy)
        {
            const string failedToAckMessage =
                "Basic ack failed because channel was closed with message '{0}'." +
                " Message remains on RabbitMQ and will be retried." +
                " ConsumerTag: {1}, DeliveryTag: {2}";

            var ackResult = AckResult.Exception;

            try
            {
                Preconditions.CheckNotNull(context.Consumer.Model, "context.Consumer.Model");

                ackResult = ackStrategy(context.Consumer.Model, context.Info.DeliverTag);
            }
            catch (AlreadyClosedException alreadyClosedException)
            {
                logger.InfoWrite(failedToAckMessage,
                                 alreadyClosedException.Message,
                                 context.Info.ConsumerTag,
                                 context.Info.DeliverTag);
            }
            catch (IOException ioException)
            {
                logger.InfoWrite(failedToAckMessage,
                                 ioException.Message,
                                 context.Info.ConsumerTag,
                                 context.Info.DeliverTag);
            }
            catch (Exception exception)
            {
                logger.ErrorWrite("Unexpected exception when attempting to ACK or NACK\n{0}", exception);
            }
            finally
            {
                eventBus.Publish(new AckEvent(context.Info, context.Properties, context.Body, ackResult));
            }
        }

        private string BuildErrorMessage(ConsumerExecutionContext context, Exception exception)
        {
            var message = Encoding.UTF8.GetString(context.Body);

            return "Exception thrown by subscription callback.\n" +
                   string.Format("\tExchange:    '{0}'\n", context.Info.Exchange) +
                   string.Format("\tRouting Key: '{0}'\n", context.Info.RoutingKey) +
                   string.Format("\tRedelivered: '{0}'\n", context.Info.Redelivered) +
                   string.Format("Message:\n{0}\n", message) +
                   string.Format("BasicProperties:\n{0}\n", context.Properties) +
                   string.Format("Exception:\n{0}\n", exception);
        }

        public void Dispose()
        {
            consumerErrorStrategy.Dispose();
        }
    }

}