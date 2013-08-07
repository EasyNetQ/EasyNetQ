using System;
using System.Collections;
using RabbitMQ.Client;

namespace EasyNetQ.InMemoryClient
{
    public class QueueInfo
    {
        public string Id { get; private set; }
        public string Name { get; private set; }        
        public bool Durable { get; private set; }        
        public bool Exclusive { get; private set; }        
        public bool AutoDelete { get; private set; }        
        public IDictionary Arguments { get; private set; }

        private readonly CircleBuffer<ConsumerInfo> consumers = new CircleBuffer<ConsumerInfo>();

        public QueueInfo(string name, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Durable = durable;
            Exclusive = exclusive;
            AutoDelete = autoDelete;
            Arguments = arguments;
        }

        public void AddConsumer(bool noAck, string consumerTag, IBasicConsumer consumer)
        {
            consumers.Add(new ConsumerInfo(noAck, consumerTag, consumer));
        }

        public void AcceptMessage(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            var consumerInfo = consumers.Next;
            consumerInfo.BasicConsumer.HandleBasicDeliver(
                consumerInfo.ConsumerTag,
                0,
                false,
                exchange,
                routingKey,
                basicProperties,
                body);
            
        }

        /// <summary>
        /// http://www.rabbitmq.com/consumer-cancel.html
        /// Fires if:
        /// a) queue is deleted
        /// b) queues main node is down. In case of HA queues in a cluster.
        /// </summary>
        public void FireConsumerCancelNotification()
        {
            foreach (var consumerInfo in consumers.CircleOnesEnumerator())
            {
                consumerInfo.BasicConsumer.HandleBasicCancel(consumerInfo.ConsumerTag);    
            }
        }
    }

    public class ConsumerInfo
    {
        public bool NoAck { get; private set; }
        public string ConsumerTag { get; private set; }
        public IBasicConsumer BasicConsumer { get; private set; }

        public ConsumerInfo(bool noAck, string consumerTag, IBasicConsumer basicConsumer)
        {
            NoAck = noAck;
            ConsumerTag = consumerTag;
            BasicConsumer = basicConsumer;
        }
    }
}