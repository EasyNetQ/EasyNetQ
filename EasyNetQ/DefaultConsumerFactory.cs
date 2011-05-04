using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class DefaultConsumerFactory : IConsumerFactory
    {
        public IBasicConsumer CreateConsumer(IModel model, MessageCallback callback)
        {
            return new CallbackConsumer(model, callback);
        }
    }
}