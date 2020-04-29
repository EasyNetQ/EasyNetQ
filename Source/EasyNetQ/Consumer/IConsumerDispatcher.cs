using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumerDispatcher : IDisposable
    {
        void QueueAction(Action action, bool surviveDisconnect = false);
        void OnDisconnected();
    }
}
