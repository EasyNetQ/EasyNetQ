using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumerDispatcherFactory : IDisposable
    {
        IConsumerDispatcher GetConsumerDispatcher();
        void OnDisconnected();
    }
}