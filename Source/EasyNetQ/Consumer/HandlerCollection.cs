using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using System.Reflection;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer
{
    public class HandlerCollection : IHandlerCollection
    {
        private readonly ILog logger = LogProvider.For<HandlerCollection>();
        
        private readonly IDictionary<Type, Func<IMessage, MessageReceivedInfo, Task>> handlers =
            new Dictionary<Type, Func<IMessage, MessageReceivedInfo, Task>>();


        public HandlerCollection()
        {
            ThrowOnNoMatchingHandler = true;
        }

        public IHandlerRegistration Add<T>(Func<IMessage<T>, MessageReceivedInfo, Task> handler) where T : class
        {
            Preconditions.CheckNotNull(handler, "handler");

            if (handlers.ContainsKey(typeof(T)))
            {
                throw new EasyNetQException("There is already a handler for message type '{0}'", typeof(T).Name);
            }

            handlers.Add(typeof(T), (iMessage, messageReceivedInfo) => handler((IMessage<T>)iMessage, messageReceivedInfo));
            return this;
        }

        public IHandlerRegistration Add<T>(Action<IMessage<T>, MessageReceivedInfo> handler) where T : class
        {
            Preconditions.CheckNotNull(handler, "handler");

            Add<T>((message, info) => TaskHelpers.ExecuteSynchronously(() => handler(message, info)));
            return this;
        }

        public Func<IMessage<T>, MessageReceivedInfo, Task> GetHandler<T>() where T : class
        {
            return GetHandler(typeof(T));
        }

        public Func<IMessage, MessageReceivedInfo, Task> GetHandler(Type messageType)
        {
            Func<IMessage, MessageReceivedInfo, Task> func;
            if (handlers.TryGetValue(messageType, out func))
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

            return (message, info) => Task.Factory.StartNew(() => { });
        }

        public bool ThrowOnNoMatchingHandler { get; set; }
    }
}