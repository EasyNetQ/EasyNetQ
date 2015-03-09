using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumer : IDisposable
    {
        IDisposable StartConsuming();
    }
}