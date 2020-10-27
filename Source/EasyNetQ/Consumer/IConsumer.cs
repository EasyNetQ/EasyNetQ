using System;

namespace EasyNetQ.Consumer
{
    /// <summary>
    ///     Represent an abstract consumer
    /// </summary>
    public interface IConsumer : IDisposable
    {
        /// <summary>
        ///     Starts the consumer
        /// </summary>
        /// <returns>Disposable to stop the consumer</returns>
        void StartConsuming();
    }
}
