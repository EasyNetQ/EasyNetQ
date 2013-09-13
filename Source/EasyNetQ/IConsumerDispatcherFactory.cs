using System;

namespace EasyNetQ
{
    public interface IConsumerDispatcherFactory : IDisposable
    {
        IConsumerDispatcher GetConsumerDispatcher();
    }
}