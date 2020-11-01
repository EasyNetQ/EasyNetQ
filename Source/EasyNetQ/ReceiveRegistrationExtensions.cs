using System;
using System.Threading.Tasks;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for IReceiveRegistration
    /// </summary>
    public static class ReceiveRegistrationExtensions
    {
        /// <summary>
        /// Add an asynchronous message handler to this receiver
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="receiveRegistration">The receive registration</param>
        /// <param name="onMessage">The message handler</param>
        /// <returns>'this' for fluent configuration</returns>
        public static IReceiveRegistration Add<T>(this IReceiveRegistration receiveRegistration, Func<T, Task> onMessage)
        {
            Preconditions.CheckNotNull(receiveRegistration, "receiveRegistration");

            return receiveRegistration.Add<T>((m, c) => onMessage(m));
        }

        /// <summary>
        /// Add an asynchronous message handler to this receiver
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="receiveRegistration">The receive registration</param>
        /// <param name="onMessage">The message handler</param>
        /// <returns>'this' for fluent configuration</returns>
        public static IReceiveRegistration Add<T>(this IReceiveRegistration receiveRegistration, Action<T> onMessage)
        {
            Preconditions.CheckNotNull(receiveRegistration, "receiveRegistration");

            var onMessageAsync = TaskHelpers.FromAction<T>((m, c) => onMessage(m));
            return receiveRegistration.Add(onMessageAsync);
        }
    }
}
