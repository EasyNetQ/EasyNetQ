using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumerDispatcher : IDisposable
    {
        void QueueAction(Action action, Priority priority = Priority.Low);
        void OnDisconnected();
    }
}
