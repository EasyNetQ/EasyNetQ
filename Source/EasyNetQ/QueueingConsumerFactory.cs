using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.v0_9_1;

namespace EasyNetQ
{
    /// <summary>
    /// This consumer factory starts a thread to handle message delivery from
    /// a shared queue. It returns a QueueingBasicConsumer with a reference to the 
    /// queue.
    /// </summary>
    public class QueueingConsumerFactory : IConsumerFactory
    {
        private readonly IEasyNetQLogger logger;
        private readonly IConsumerErrorStrategy consumerErrorStrategy;
        private readonly IConsumerDispatcherFactory consumerDispatcherFactory;

        private readonly IDictionary<string, SubscriptionInfo> subscriptions = 
            new ConcurrentDictionary<string, SubscriptionInfo>();
        
        // useful for testing, called when DoAck is called after a message is handled
        public Action SynchronisationAction { get; set; }

        public QueueingConsumerFactory(
            IEasyNetQLogger logger, 
            IConsumerErrorStrategy consumerErrorStrategy, 
            IConsumerDispatcherFactory consumerDispatcherFactory)
        {
            this.logger = logger;
            this.consumerErrorStrategy = consumerErrorStrategy;
            this.consumerDispatcherFactory = consumerDispatcherFactory;
        }

        public void HandleMessageDelivery(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var consumerTag = basicDeliverEventArgs.ConsumerTag;
            if (consumerTag == null)
            {
                logger.ErrorWrite("BasicDeliverEventArgs.ConsumerTag is null");
                return;
            }
            if (!subscriptions.ContainsKey(consumerTag))
            {
                logger.ErrorWrite("No subscription for ConsumerTag: {0}", consumerTag);
                return;
            }

            var subscriptionInfo = subscriptions[consumerTag];
            if (!subscriptionInfo.Consumer.IsRunning)
            {
                // this message's consumer has stopped, so just return
                logger.DebugWrite("Consumer has stopped running. ConsumerTag: {0}", consumerTag);
                return;
            }

            logger.DebugWrite("Recieved \n\tRoutingKey: '{0}'\n\tCorrelationId: '{1}'\n\tConsumerTag: '{2}'", 
                basicDeliverEventArgs.RoutingKey, 
                basicDeliverEventArgs.BasicProperties.CorrelationId,
                consumerTag);

            try
            {
                var completionTask = subscriptionInfo.Callback(
                    consumerTag, 
                    basicDeliverEventArgs.DeliveryTag, 
                    basicDeliverEventArgs.Redelivered,
                    basicDeliverEventArgs.Exchange, 
                    basicDeliverEventArgs.RoutingKey,
                    basicDeliverEventArgs.BasicProperties, 
                    basicDeliverEventArgs.Body);

                if (completionTask.Status == TaskStatus.Created)
                {
                    logger.ErrorWrite("Task returned from consumer callback is not started. ConsumerTag: '{0}'",
                        subscriptionInfo.Consumer.ConsumerTag);
                }

                completionTask.ContinueWith(task =>
                {
                    if(task.IsFaulted)
                    {
                        var exception = task.Exception;
                        HandleErrorInSubscriptionHandler(basicDeliverEventArgs, subscriptionInfo, exception);
                    }
                    else
                    {
                        DoAck(basicDeliverEventArgs, subscriptionInfo, SuccessAckStrategy);
                    }
                });
            }
            catch (Exception exception)
            {
                HandleErrorInSubscriptionHandler(basicDeliverEventArgs, subscriptionInfo, exception);
            }
            finally
            {
                if(subscriptionInfo.ModelIsSingleUse)
                {
                    subscriptions.Remove(consumerTag);
                }
            }
        }

        private void HandleErrorInSubscriptionHandler(
            BasicDeliverEventArgs basicDeliverEventArgs,
            SubscriptionInfo subscriptionInfo,
            Exception exception)
        {
            logger.ErrorWrite(BuildErrorMessage(basicDeliverEventArgs, exception));
            consumerErrorStrategy.HandleConsumerError(basicDeliverEventArgs, exception);
            DoAck(basicDeliverEventArgs, subscriptionInfo, ExceptionAckStrategy);
        }

        private void DoAck(BasicDeliverEventArgs basicDeliverEventArgs, SubscriptionInfo subscriptionInfo, Action<IModel, ulong> ackStrategy)
        {
            const string failedToAckMessage = "Basic ack failed because channel was closed with message {0}." +
                                              " Message remains on RabbitMQ and will be retried.";

            try
            {
                ackStrategy(subscriptionInfo.Consumer.Model, basicDeliverEventArgs.DeliveryTag);

                if (subscriptionInfo.ModelIsSingleUse)
                {
                    subscriptionInfo.Consumer.CloseModel();
                    subscriptionInfo.SubscriptionAction.ClearAction();
                }
            }
            catch (AlreadyClosedException alreadyClosedException)
            {
                logger.InfoWrite(failedToAckMessage, alreadyClosedException.Message);
            }
            catch (IOException ioException)
            {
                logger.InfoWrite(failedToAckMessage, ioException.Message);
            }
            finally
            {
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

        private string BuildErrorMessage(BasicDeliverEventArgs basicDeliverEventArgs, Exception exception)
        {
            var message = Encoding.UTF8.GetString(basicDeliverEventArgs.Body);

            var properties = basicDeliverEventArgs.BasicProperties as BasicProperties;
            var propertiesMessage = new StringBuilder();
            if (properties != null)
            {
                properties.AppendPropertyDebugStringTo(propertiesMessage);
            }

            return "Exception thrown by subscription calback.\n" +
                   string.Format("\tExchange:    '{0}'\n", basicDeliverEventArgs.Exchange) +
                   string.Format("\tRouting Key: '{0}'\n", basicDeliverEventArgs.RoutingKey) +
                   string.Format("\tRedelivered: '{0}'\n", basicDeliverEventArgs.Redelivered) +
                   string.Format("Message:\n{0}\n", message) +
                   string.Format("BasicProperties:\n{0}\n", propertiesMessage) +
                   string.Format("Exception:\n{0}\n", exception);
        }

        public DefaultBasicConsumer CreateConsumer(
            SubscriptionAction subscriptionAction,
            IModel model, 
            bool modelIsSingleUse, 
            MessageCallback callback)
        {
            var dispatcher = consumerDispatcherFactory.GetConsumerDispatcher();

            var consumer = new EasyNetQConsumer(model, 
                deliverEventArgs => 
                    dispatcher.QueueAction(() => 
                        HandleMessageDelivery(deliverEventArgs)))
                {
                    ConsumerTag = subscriptionAction.Id
                };

            if (subscriptions.ContainsKey(consumer.ConsumerTag))
            {
                logger.DebugWrite("Removing existing subscription with ConsumerTag: " + consumer.ConsumerTag);
                subscriptions.Remove(consumer.ConsumerTag);
            }

            subscriptions.Add(consumer.ConsumerTag, new SubscriptionInfo(subscriptionAction, consumer, callback, modelIsSingleUse, model));
            return consumer;
        }

        public void ClearConsumers()
        {
            subscriptions.Clear();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            consumerErrorStrategy.Dispose();
            consumerDispatcherFactory.Dispose();

            foreach (var subscriptionInfo in subscriptions)
            {
                subscriptionInfo.Value.Channel.Close();
            }

            disposed = true;
        }
    }

    public class SubscriptionInfo
    {
        public SubscriptionAction SubscriptionAction { get; set; }
        public EasyNetQConsumer Consumer { get; private set; }
        public MessageCallback Callback { get; private set; }
        public bool ModelIsSingleUse { get; private set; }
        public IModel Channel { get; private set; }

        public SubscriptionInfo(SubscriptionAction subscriptionAction, EasyNetQConsumer consumer, MessageCallback callback, bool modelIsSingleUse, IModel channel)
        {
            SubscriptionAction = subscriptionAction;
            Consumer = consumer;
            Callback = callback;
            ModelIsSingleUse = modelIsSingleUse;
            Channel = channel;
        }
    }
}