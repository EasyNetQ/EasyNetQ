using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class CallbackConsumer : DefaultBasicConsumer
    {
        private readonly Action<string, ulong, bool, string, string, IBasicProperties, byte[]> callback;

        public CallbackConsumer(IModel model, Action<string, ulong, bool, string, string, IBasicProperties, byte[]> callback) 
            : base(model)
        {
            this.callback = callback;
        }

        public override void HandleBasicDeliver(
            string consumerTag, 
            ulong deliveryTag, 
            bool redelivered, 
            string exchange, 
            string routingKey, 
            IBasicProperties properties, 
            byte[] body)
        {
            callback(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
        }
    }
}