using System;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    public interface IConsumerErrorStrategy : IDisposable
    {
        void HandleConsumerError(BasicDeliverEventArgs devliverArgs, Exception exception);
    }
}