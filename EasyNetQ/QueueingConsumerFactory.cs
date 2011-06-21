using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
        private SharedQueue sharedQueue = new SharedQueue();
        private readonly IDictionary<string, MessageCallback> callbacks = 
            new Dictionary<string, MessageCallback>();
        private readonly object sharedQueueLock = new object();

        public QueueingConsumerFactory()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while(true)
                {
                    try
                    {
                        BasicDeliverEventArgs deliverEventArgs;
                        lock (sharedQueueLock)
                        {
                            deliverEventArgs = (BasicDeliverEventArgs)sharedQueue.DequeueNoWait(null);
                        }
                        if(deliverEventArgs != null)
                        {
                            HandleMessageDelivery(deliverEventArgs);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        Console.WriteLine("QueueingConsumerFactory -> EndOfStreamException");
                        // do nothing here, EOS fired when queue is closed
                    }
                    Thread.Sleep(0);
                }
            });
        }

        private void HandleMessageDelivery(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var consumerTag = basicDeliverEventArgs.ConsumerTag;
            if (!callbacks.ContainsKey(consumerTag))
            {
                throw new EasyNetQException("No callback found for ConsumerTag {0}", consumerTag);
            }

            var callback = callbacks[consumerTag];
            callback(
                consumerTag, 
                basicDeliverEventArgs.DeliveryTag, 
                basicDeliverEventArgs.Redelivered,
                basicDeliverEventArgs.Exchange, 
                basicDeliverEventArgs.RoutingKey,
                basicDeliverEventArgs.BasicProperties, 
                basicDeliverEventArgs.Body);
        }

        public DefaultBasicConsumer CreateConsumer(IModel model, MessageCallback callback)
        {
            var consumer = new QueueingBasicConsumer(model, sharedQueue);
            var consumerTag = Guid.NewGuid().ToString();
            consumer.ConsumerTag = consumerTag;
            callbacks.Add(consumerTag, callback);
            return consumer;
        }

        public void ClearConsumers()
        {
            callbacks.Clear();
            Console.WriteLine("Waiting for ClearConsumers lock");
            lock (sharedQueueLock)
            {
                Console.WriteLine("Got ClearConsumers lock");
                sharedQueue = new SharedQueue();
            }
        }
    }
}