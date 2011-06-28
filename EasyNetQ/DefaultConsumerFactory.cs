using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class DefaultConsumerFactory : IConsumerFactory
    {
        public DefaultBasicConsumer CreateConsumer(IModel model, MessageCallback callback)
        {
            return new CallbackConsumer(model, callback);
        }

        public void ClearConsumers()
        {
            // nothing to do
        }

        public void Dispose()
        {
            // nothing to do
        }
    }
}