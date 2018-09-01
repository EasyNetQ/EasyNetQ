using System;
using System.Threading.Tasks;
using EasyNetQ.Internals;

namespace EasyNetQ.Consumer
{
    public static class HandlerRegistrationExtensions
    {
        /// <summary>
        /// Add an asynchronous handler
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="handlerRegistration">The handler registration</param>
        /// <param name="handler">The handler</param>
        /// <returns></returns>
        public static IHandlerRegistration Add<T>(
            this IHandlerRegistration handlerRegistration,
            Action<IMessage<T>, MessageReceivedInfo> handler
        )
        {
            Preconditions.CheckNotNull(handlerRegistration, "handlerRegistration");

            var asyncHandler = TaskHelpers.FromAction<IMessage<T>, MessageReceivedInfo>((m, i, c) => handler(m, i));
            return handlerRegistration.Add(asyncHandler);
        }
        
        /// <summary>
        /// Add an asynchronous handler
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="handlerRegistration">The handler registration</param>
        /// <param name="handler">The handler</param>
        /// <returns></returns>
        public static IHandlerRegistration Add<T>(
            this IHandlerRegistration handlerRegistration,
            Func<IMessage<T>, MessageReceivedInfo, Task> handler
        )
        {
            Preconditions.CheckNotNull(handlerRegistration, "handlerRegistration");

            return handlerRegistration.Add<T>((m, i, c) => handler(m, i));
        }
    }
}