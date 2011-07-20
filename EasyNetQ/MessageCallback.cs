using RabbitMQ.Client;

namespace EasyNetQ
{
    public delegate void MessageCallback(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body);
}