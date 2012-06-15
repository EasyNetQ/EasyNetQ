using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConsumerFactory : IDisposable
    {
        DefaultBasicConsumer CreateConsumer(SubscriptionAction subscriptionAction, IModel model, bool modelIsSingleUse, MessageCallback callback);
        void ClearConsumers();
    }
}