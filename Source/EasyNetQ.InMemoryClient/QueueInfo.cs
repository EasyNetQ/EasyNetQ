using System.Collections;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace EasyNetQ.InMemoryClient
{
    public class QueueInfo
    {
        public string Name { get; private set; }        
        public bool Durable { get; private set; }        
        public bool Exclusive { get; private set; }        
        public bool AutoDelete { get; private set; }        
        public IDictionary Arguments { get; private set; }

        private readonly IList<ConsumerInfo> consumers = new List<ConsumerInfo>();

        public QueueInfo(string name, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
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
            foreach (var consumerInfo in consumers)
            {
                consumerInfo.BasicConsumer.HandleBasicDeliver(
                    consumerInfo.ConsumerTag,
                    0,
                    false,
                    exchange,
                    routingKey,
                    basicProperties,
                    body);
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