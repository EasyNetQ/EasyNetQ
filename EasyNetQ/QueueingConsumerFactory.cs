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
        private readonly SharedQueue sharedQueue = new SharedQueue();
        private readonly IDictionary<string, MessageCallback> callbacks = 
            new Dictionary<string, MessageCallback>();

        public QueueingConsumerFactory()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while(true)
                    {
                        HandleMessageDelivery((BasicDeliverEventArgs)sharedQueue.Dequeue());
                    }
                }
                catch (EndOfStreamException)
                {
                    // do nothing here, EOS fired when queue is closed
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
    }
}