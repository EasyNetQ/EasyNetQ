using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumer : IDisposable
    {
        void StartConsuming();
        event Action<IConsumer> RemoveMeFromList;
    }
}