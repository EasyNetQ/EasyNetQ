using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    public interface IReceiveRegistration
    {
        /// <summary>
        /// Add an asynchronous message handler to this receiver
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="onMessage">The message handler</param>
        /// <returns>'this' for fluent configuration</returns>
        IReceiveRegistration Add<T>(Func<T, CancellationToken, Task> onMessage);
    }
}