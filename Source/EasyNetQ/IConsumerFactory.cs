using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConsumerFactory : IDisposable
    {
        DefaultBasicConsumer CreateConsumer(IModel model, bool modelIsSingleUse, MessageCallback callback);
        void ClearConsumers();
    }
}