using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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
        private readonly BlockingCollection<BasicDeliverEventArgs> queue = 
            new BlockingCollection<BasicDeliverEventArgs>(new ConcurrentQueue<BasicDeliverEventArgs>());

        private readonly IDictionary<string, SubscriptionInfo> subscriptions = 
            new ConcurrentDictionary<string, SubscriptionInfo>();
        
        private readonly Thread subscriptionCallbackThread;

        public QueueingConsumerFactory(IEasyNetQLogger logger, IConsumerErrorStrategy consumerErrorStrategy)
        {
            this.logger = logger;
            this.consumerErrorStrategy = consumerErrorStrategy;

            // start the subscription callback thread
            // all subscription actions registered with Subscribe or Request
            // run here.
            subscriptionCallbackThread = new Thread(_ =>
            {
                try
                {
                    while(true)
                    {
                        if (disposed) break;

                        HandleMessageDelivery(queue.Take());
                    }
                }
                catch (InvalidOperationException)
                {
                    // InvalidOperationException is thrown when Take is called after 
                    // queue.CompleteAdding(), this is signals that this class is being
                    // disposed, so we allow the thread to complete.
                }
            });
            subscriptionCallbackThread.Start();
        }

        private void HandleMessageDelivery(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var consumerTag = basicDeliverEventArgs.ConsumerTag;
            if (!subscriptions.ContainsKey(consumerTag))
            {
                logger.DebugWrite("No subscription for consumerTag: {0}", consumerTag);
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

                completionTask.ContinueWith(task =>
                {
                    if(task.IsFaulted)
                    {
                        var exception = task.Exception;
                        HandleErrorInSubscriptionHandler(basicDeliverEventArgs, subscriptionInfo, exception);
                    }
                    else
                    {
                        DoAck(basicDeliverEventArgs, subscriptionInfo);
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
            DoAck(basicDeliverEventArgs, subscriptionInfo);
        }

        private void DoAck(BasicDeliverEventArgs basicDeliverEventArgs, SubscriptionInfo subscriptionInfo)
        {
            const string failedToAckMessage = "Basic ack failed because channel was closed with message {0}." +
                                              " Message remains on RabbitMQ and will be retried.";

            try
            {
                subscriptionInfo.Consumer.Model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
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
            var consumer = new EasyNetQConsumer(model, queue);
            var consumerTag = Guid.NewGuid().ToString();
            consumer.ConsumerTag = consumerTag;
            subscriptions.Add(consumerTag, new SubscriptionInfo(subscriptionAction, consumer, callback, modelIsSingleUse, model));

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
            queue.CompleteAdding();

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