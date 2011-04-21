using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;

namespace Mike.AmqpSpike
{
    public class PublishSubscribe
    {
        private const string subscription = "mike.subscription.1";
        private const string defaultExchange = "";

        public void CreateExchange()
        {
            WithChannel.Do(channel => channel.ExchangeDeclare(
                exchange: subscription, 
                type: ExchangeType.Direct, 
                durable: true));
        }

        public void PublishMessage()
        {
            WithChannel.Do(channel =>
            {
                var defaultProperties = channel.CreateBasicProperties();
                var messageText = "Hello World " + Guid.NewGuid().ToString().Substring(0, 5);
                var message = Encoding.UTF8.GetBytes(messageText);

                channel.BasicPublish(subscription, subscription, defaultProperties, message);
                Console.WriteLine("Published Message '{0}'", messageText);
            });
        }

        public void GetMessagesForA()
        {
            GetMessagesFor(subscriberA);
        }

        public void GetMessagesForB()
        {
            GetMessagesFor(subscriberB);
        }

        private const string subscriberA = "subscriber.a";
        public void SubscribeA()
        {
            Subscribe(subscriberA);
        }

        private const string subscriberB = "subscriber.b";
        public void SubscribeB()
        {
            Subscribe(subscriberB);
        }

        public void Subscribe(string subscriberName)
        {
            WithChannel.Do(channel =>
            {
                var queue = channel.QueueDeclare(subscriberName, 
                                                 durable: true, 
                                                 exclusive: false, 
                                                 autoDelete: false, 
                                                 arguments: null);

                channel.QueueBind(queue, subscription, subscription);
                Console.WriteLine("{0}: {1}", subscriberName, queue);
            });
        }

        public void GetMessagesFor(string subscriber)
        {
            WithChannel.Do(channel =>
            {
                var consumer = new CallbackConsumer();
                channel.BasicConsume(subscriber, true, consumer);

                // give the consumer some time to get messages
                Thread.Sleep(1000);
            });
            Console.WriteLine("Stopped consuming messages");
        }
    }
}