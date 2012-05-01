using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;

namespace Mike.AmqpSpike
{
    public class TopicSpike
    {
        private const string exchange = "topic.spike.1";

        public void TopicPublishA()
        {
            Publish("X.A");
        }

        public void TopicPublishB()
        {
            Publish("X.B");
        }

        public void TopicSubcribeA()
        {
            TopicSubscribe("X.A", "topic.spike.A");
        }

        public void TopicSubscribeB()
        {
            TopicSubscribe("X.#", "topic.spike.B");
        }

        private static void Publish(string routingKey)
        {
            WithChannel.Do(channel =>
            {
                channel.ExchangeDeclare(
                    exchange: exchange,
                    type: ExchangeType.Topic,
                    durable: true);

                var defaultProperties = channel.CreateBasicProperties();
                var messageText = "Hello World " + Guid.NewGuid().ToString().Substring(0, 5);
                var message = Encoding.UTF8.GetBytes(messageText);

                channel.BasicPublish(exchange, routingKey, defaultProperties, message);
                Console.WriteLine("Published Message '{0}'", messageText);
            });
        }

        private static void TopicSubscribe(string routingKey, string queueName)
        {
            WithChannel.Do(channel =>
            {
                var queue = channel.QueueDeclare(queueName,
                                                 durable: true,
                                                 exclusive: false,
                                                 autoDelete: false,
                                                 arguments: null);

                channel.QueueBind(queue, exchange, routingKey);
                Console.WriteLine("{0}: {1}", queueName, queue);

                var consumer = new CallbackConsumer();
                channel.BasicConsume(queue, true, consumer);

                // give the consumer some time to get messages
                Thread.Sleep(1000);

                Console.WriteLine("Stopped consuming");
            });
        }
    }
}