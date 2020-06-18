using EasyNetQ.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Consumer
{
    public class HandlerCollection : IHandlerCollection
    {
        private readonly ILog logger = LogProvider.For<HandlerCollection>();

        private readonly IDictionary<Type, Func<IMessage, MessageReceivedInfo, CancellationToken, Task>> handlers =
            new Dictionary<Type, Func<IMessage, MessageReceivedInfo, CancellationToken, Task>>();

        public HandlerCollection()
        {
            ThrowOnNoMatchingHandler = true;
        }

        /// <inheritdoc />
        public IHandlerRegistration Add<T>(Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> handler)
        {
            Preconditions.CheckNotNull(handler, "handler");

            if (handlers.ContainsKey(typeof(T)))
            {
                throw new EasyNetQException("There is already a handler for message type '{0}'", typeof(T).Name);
            }

            handlers.Add(typeof(T), (m, i, c) => handler((IMessage<T>)m, i, c));
            return this;
        }

        /// <inheritdoc />
        public Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> GetHandler<T>()
        {
            return GetHandler(typeof(T));
        }

        /// <inheritdoc />
        public Func<IMessage, MessageReceivedInfo, CancellationToken, Task> GetHandler(Type messageType)
        {
            if (handlers.TryGetValue(messageType, out var func))
            {
                return func;
            }

            // no exact handler match found, so let's see if we can find a handler that
            // handles a supertype of the consumed message.
            var handlerType = handlers.Keys.FirstOrDefault(type => type.IsAssignableFrom(messageType));
            if (handlerType != null)
            {
                return handlers[handlerType];
            }

            if (ThrowOnNoMatchingHandler)
            {
                logger.ErrorFormat("No handler found for message type {messageType}", messageType.Name);
                throw new EasyNetQException("No handler found for message type {0}", messageType.Name);
            }

            return (m, i, c) => Task.CompletedTask;
        }

        /// <inheritdoc />
        public bool ThrowOnNoMatchingHandler { get; set; }
    }
}
