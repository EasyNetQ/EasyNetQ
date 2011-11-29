using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConsumerFactory : IDisposable
    {
        DefaultBasicConsumer CreateConsumer(IModel model, MessageCallback callback);
        void ClearConsumers();
    }
}