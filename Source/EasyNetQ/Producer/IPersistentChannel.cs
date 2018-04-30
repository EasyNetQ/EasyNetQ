using System;
using System.Threading;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public interface IPersistentChannel : IDisposable
    {
        T InvokeChannelAction<T>(Func<IModel, T> channelAction, CancellationToken cancellation = default(CancellationToken));
        void InvokeChannelAction(Action<IModel> channelAction, CancellationToken cancellation = default(CancellationToken));
    }
}