using System;
using System.Text;
using RabbitMQ.Client;

namespace Mike.AmqpSpike
{
    public class CallbackConsumer : DefaultBasicConsumer
    {
        public override void HandleBasicDeliver(
            string consumerTag, 
            ulong deliveryTag, 
            bool redelivered, 
            string exchange, 
            string routingKey, 
            IBasicProperties properties, 
            byte[] body)
        {
            Console.WriteLine("--- in HandleBasicDeliver ------ ");
            Console.Out.WriteLine("consumerTag = {0}", consumerTag);
            Console.Out.WriteLine("deliveryTag = {0}", deliveryTag);
            Console.Out.WriteLine("redelivered = {0}", redelivered);
            Console.Out.WriteLine("exchange = {0}", exchange);
            Console.Out.WriteLine("routingKey = {0}", routingKey);

            var messageText = Encoding.UTF8.GetString(body);
            Console.Out.WriteLine("messageText = {0}", messageText);
        }
    }
}