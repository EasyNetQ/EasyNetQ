using System;
using System.Threading.Tasks;

namespace EasyNetQ.Consumer
{
    public interface IReceiveRegistration
    {
        /// <summary>
        /// Add an asynchronous message handler to this receiver
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="onMessage">The message handler</param>
        /// <returns>'this' for fluent configuration</returns>
        IReceiveRegistration Add<T>(Func<T, Task> onMessage) where T : class;

        /// <summary>
        /// Add a message handler to this receiver
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="onMessage">The message handler</param>
        /// <returns>'this' for fluent configuration</returns>
        IReceiveRegistration Add<T>(Action<T> onMessage) where T : class;
    }


}