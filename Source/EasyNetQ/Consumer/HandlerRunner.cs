using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
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

        // useful for testing, called when DoAck is called after a message is handled
        public Action SynchronisationAction { get; set; }

        public HandlerRunner(IEasyNetQLogger logger, IConsumerErrorStrategy consumerErrorStrategy)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(consumerErrorStrategy, "consumerErrorStrategy");

            this.logger = logger;
            this.consumerErrorStrategy = consumerErrorStrategy;
        }

        public void InvokeUserMessageHandler(ConsumerExecutionContext context)
        {
            Preconditions.CheckNotNull(context, "context");

            logger.DebugWrite("Recieved \n\tRoutingKey: '{0}'\n\tCorrelationId: '{1}'\n\tConsumerTag: '{2}'" +
                "\n\tDeliveryTag: {3}\n\tRedelivered: {4}",
                context.Info.RoutingKey,
                context.Properties.CorrelationId,
                context.Info.ConsumerTag,
                context.Info.DeliverTag,
                context.Info.Redelivered);

            try
            {
                var completionTask = context.UserHandler(context.Body, context.Properties, context.Info);

                if (completionTask.Status == TaskStatus.Created)
                {
                    logger.ErrorWrite("Task returned from consumer callback is not started. ConsumerTag: '{0}'",
                        context.Info.ConsumerTag);
                }
                else
                {
                    completionTask.ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            var exception = task.Exception;
                            HandleErrorInSubscriptionHandler(context, exception);
                        }
                        else
                        {
                            DoAck(context, SuccessAckStrategy);
                        }
                    });
                }
            }
            catch (Exception exception)
            {
                HandleErrorInSubscriptionHandler(context, exception);
            }

        }

        private void HandleErrorInSubscriptionHandler(ConsumerExecutionContext context,
            Exception exception)
        {
            logger.ErrorWrite(BuildErrorMessage(context, exception));
            try
            {
                consumerErrorStrategy.HandleConsumerError(context, exception);
                DoAck(context, ExceptionAckStrategy);
            }
            catch (Exception consumerErrorStrategyError)
            {
                logger.ErrorWrite("Exception in ConsumerErrorStrategy:\n{0}", 
                    consumerErrorStrategyError);
            }
        }

        private void DoAck(ConsumerExecutionContext context , Action<IModel, ulong> ackStrategy)
        {
            const string failedToAckMessage = 
                "Basic ack failed because channel was closed with message '{0}'." +
                " Message remains on RabbitMQ and will be retried." + 
                " ConsumerTag: {1}, DeliveryTag: {2}";

            try
            {
                Preconditions.CheckNotNull(context.Consumer.Model, "context.Consumer.Model");

                ackStrategy(context.Consumer.Model, context.Info.DeliverTag);
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
                try
                {
                    foreach (var postAckCallback in context.PostAckCallbacks)
                    {
                        postAckCallback();
                    }
                }
                catch (Exception exception)
                {
                    logger.ErrorWrite("Exception in PostAckCallback:\n{0}", exception);
                }

                if (SynchronisationAction != null)
                {
                    SynchronisationAction();
                }
            }
        }

        private void SuccessAckStrategy(IModel model, ulong deliveryTag)
        {
            model.BasicAck(deliveryTag, false);
        }

        private void ExceptionAckStrategy(IModel model, ulong deliveryTag)
        {
            switch (consumerErrorStrategy.PostExceptionAckStrategy())
            {
                case PostExceptionAckStrategy.ShouldAck:
                    model.BasicAck(deliveryTag, false);
                    break;
                case PostExceptionAckStrategy.ShouldNackWithoutRequeue:
                    model.BasicNack(deliveryTag, false, false);
                    break;
                case PostExceptionAckStrategy.ShouldNackWithRequeue:
                    model.BasicNack(deliveryTag, false, true);
                    break;
                case PostExceptionAckStrategy.DoNothing:
                    break;
            }
        }

        private string BuildErrorMessage(ConsumerExecutionContext context, Exception exception)
        {
            var message = Encoding.UTF8.GetString(context.Body);

            var properties = context.Properties;
            var propertiesMessage = new StringBuilder();
            if (properties != null)
            {
                properties.AppendPropertyDebugStringTo(propertiesMessage);
            }

            return "Exception thrown by subscription calback.\n" +
                   string.Format("\tExchange:    '{0}'\n", context.Info.Exchange) +
                   string.Format("\tRouting Key: '{0}'\n", context.Info.RoutingKey) +
                   string.Format("\tRedelivered: '{0}'\n", context.Info.Redelivered) +
                   string.Format("Message:\n{0}\n", message) +
                   string.Format("BasicProperties:\n{0}\n", propertiesMessage) +
                   string.Format("Exception:\n{0}\n", exception);
        }

        public void Dispose()
        {
            consumerErrorStrategy.Dispose();
        }
    }
}