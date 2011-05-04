using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConsumerFactory
    {
        IBasicConsumer CreateConsumer(IModel model, MessageCallback callback);
    }
}