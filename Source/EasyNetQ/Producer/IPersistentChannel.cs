using System;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public interface IPersistentChannel : IDisposable
    {
        void InvokeChannelAction(Action<IModel> channelAction);
    }
}