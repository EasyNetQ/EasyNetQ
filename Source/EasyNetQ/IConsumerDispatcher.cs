using System;

namespace EasyNetQ
{
    public interface IConsumerDispatcher : IDisposable
    {
        void QueueAction(Action action);
    }
}