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
using RabbitMQ.Util;

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
        private SharedQueue sharedQueue = new SharedQueue();

        private readonly IDictionary<string, SubscriptionInfo> subscriptions = 
            new ConcurrentDictionary<string, SubscriptionInfo>();
        
        private readonly object sharedQueueLock = new object();
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
                while(true)
                {
                    if (disposed) break;                    

                    try
                    {
                        BasicDeliverEventArgs deliverEventArgs;
                        lock (sharedQueueLock)
                        {
                            deliverEventArgs = (BasicDeliverEventArgs)sharedQueue.Dequeue();
                        }
                        if(deliverEventArgs != null)
                        {
                            HandleMessageDelivery(deliverEventArgs);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        // do nothing here, EOS fired when queue is closed
                        // Looks like the connection has gone away, so wait a little while
                        // before continuing to poll the queue
                        Thread.Sleep(10);
                    }
                }
            });
            subscriptionCallbackThread.Start();
        }

        private void HandleMessageDelivery(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var consumerTag = basicDeliverEventArgs.ConsumerTag;
            if (!subscriptions.ContainsKey(consumerTag))
            {
                throw new EasyNetQException("No callback found for ConsumerTag {0}", consumerTag);
            }

            logger.DebugWrite("Subscriber Recieved {0}, CorrelationId {1}", 
                basicDeliverEventArgs.RoutingKey, basicDeliverEventArgs.BasicProperties.CorrelationId);

            var subscriptionInfo = subscriptions[consumerTag];


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
                        logger.ErrorWrite(BuildErrorMessage(basicDeliverEventArgs, exception));
                        consumerErrorStrategy.HandleConsumerError(basicDeliverEventArgs, exception);
                    }
                    DoAck(basicDeliverEventArgs, subscriptionInfo);
                });
            }
            catch (Exception exception)
            {
                logger.ErrorWrite(BuildErrorMessage(basicDeliverEventArgs, exception));
                consumerErrorStrategy.HandleConsumerError(basicDeliverEventArgs, exception);
                DoAck(basicDeliverEventArgs, subscriptionInfo);
            }
        }

        private void DoAck(BasicDeliverEventArgs basicDeliverEventArgs, SubscriptionInfo subscriptionInfo)
        {
            const string failedToAckMessage = "Basic ack failed because chanel was closed with message {0}." +
                                              " Message remains on RabbitMQ and will be retried.";

            try
            {
                subscriptionInfo.Consumer.Model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
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

        public DefaultBasicConsumer CreateConsumer(IModel model, MessageCallback callback)
        {
            var consumer = new QueueingBasicConsumer(model, sharedQueue);
            var consumerTag = Guid.NewGuid().ToString();
            consumer.ConsumerTag = consumerTag;
            subscriptions.Add(consumerTag, new SubscriptionInfo(consumer, callback));
            return consumer;
        }

        public void ClearConsumers()
        {
            sharedQueue.Close(); // Dequeue will stop blocking and throw an EndOfStreamException

            lock (sharedQueueLock)
            {
                logger.DebugWrite("Clearing consumer subscriptions");
                sharedQueue = new SharedQueue();
                subscriptions.Clear();
            }
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            consumerErrorStrategy.Dispose();
            sharedQueue.Close();
            disposed = true;
        }
    }

    public class SubscriptionInfo
    {
        public IBasicConsumer Consumer { get; private set; }
        public MessageCallback Callback { get; private set; }

        public SubscriptionInfo(IBasicConsumer consumer, MessageCallback callback)
        {
            Consumer = consumer;
            Callback = callback;
        }
    }
}