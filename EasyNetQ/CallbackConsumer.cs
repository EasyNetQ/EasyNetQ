using RabbitMQ.Client;

namespace EasyNetQ
{
    public delegate void MessageCallback(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body);

    public class CallbackConsumer : DefaultBasicConsumer
    {
        private readonly MessageCallback callback;

        public CallbackConsumer(IModel model, MessageCallback callback) 
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