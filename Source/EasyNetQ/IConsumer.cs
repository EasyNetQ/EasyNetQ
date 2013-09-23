using System;

namespace EasyNetQ
{
    public interface IConsumer : IDisposable
    {
        void StartConsuming();
    }
}