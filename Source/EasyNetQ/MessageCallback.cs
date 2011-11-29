using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public delegate Task MessageCallback(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body);
}