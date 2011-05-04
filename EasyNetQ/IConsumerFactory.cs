using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConsumerFactory
    {
        DefaultBasicConsumer CreateConsumer(IModel model, MessageCallback callback);
    }
}