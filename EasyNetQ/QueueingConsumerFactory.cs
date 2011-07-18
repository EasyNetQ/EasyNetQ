using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
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
        private SharedQueue sharedQueue = new SharedQueue();

        private readonly IDictionary<string, SubscriptionInfo> subscriptions = 
            new Dictionary<string, SubscriptionInfo>();
        
        private readonly object sharedQueueLock = new object();
        private readonly Thread subscriptionCallbackThread;

        public QueueingConsumerFactory(IEasyNetQLogger logger)
        {
            this.logger = logger;
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

            logger.DebugWrite("HandleMessageDelivery '{0}'", basicDeliverEventArgs.RoutingKey);

            var subscriptionInfo = subscriptions[consumerTag];
            subscriptionInfo.Callback(
                consumerTag, 
                basicDeliverEventArgs.DeliveryTag, 
                basicDeliverEventArgs.Redelivered,
                basicDeliverEventArgs.Exchange, 
                basicDeliverEventArgs.RoutingKey,
                basicDeliverEventArgs.BasicProperties, 
                basicDeliverEventArgs.Body);

            try
            {
                subscriptionInfo.Consumer.Model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
            }
            catch (AlreadyClosedException exception)
            {
                logger.InfoWrite("Basic ack failed because chanel was closed with message {0}." + 
                    " Message remains on RabbitMQ and will be retried.", exception.Message);
            }
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
            subscriptions.Clear();
            sharedQueue.Close(); // Dequeue will stop blocking and throw an EndOfStreamException

            lock (sharedQueueLock)
            {
                logger.DebugWrite("Clearing consumer subscriptions");
                sharedQueue = new SharedQueue();
            }
            //Console.WriteLine("Cleared ClearConsumers lock");
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
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