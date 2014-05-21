using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyNetQ.Consumer
{
    public class HandlerCollection : IHandlerCollection
    {
//        private readonly IDictionary<Type, Func<IMessage<object>, MessageReceivedInfo, Task>> handlers = 
//            new Dictionary<Type, Func<IMessage<object>, MessageReceivedInfo, Task>>();

        private readonly IDictionary<Type, object> handlers = new Dictionary<Type, object>(); 
        
        private readonly IEasyNetQLogger logger;

        public HandlerCollection(IEasyNetQLogger logger)
        {
            Preconditions.CheckNotNull(logger, "logger");

            this.logger = logger;
            ThrowOnNoMatchingHandler = true;
        }

        public IHandlerRegistration Add<T>(Func<IMessage<T>, MessageReceivedInfo, Task> handler) where T : class
        {
            Preconditions.CheckNotNull(handler, "handler");

            if (handlers.ContainsKey(typeof (T)))
            {
                throw new EasyNetQException("There is already a handler for message type '{0}'", typeof(T).Name);
            }

            handlers.Add(typeof(T), handler);
            return this;
        }

        public IHandlerRegistration Add<T>(Action<IMessage<T>, MessageReceivedInfo> handler) where T : class
        {
            Preconditions.CheckNotNull(handler, "handler");

            Add<T>((message, info) => TaskHelpers.ExecuteSynchronously(() => handler(message, info)));
            return this;
        }

        // NOTE: refactoring tools might suggest this method is never invoked. Ignore them it
        // _is_ invoked by the GetHandler(Type messsageType) method below by reflection.
        public Func<IMessage<T>, MessageReceivedInfo, Task> GetHandler<T>() where T : class
        {
            // return (Func<IMessage<T>, MessageReceivedInfo, Task>)GetHandler(typeof(T));
            var messageType = typeof (T);

            if (handlers.ContainsKey(messageType))
            {
                return (Func<IMessage<T>, MessageReceivedInfo, Task>)handlers[messageType];
            }

            // no exact handler match found, so let's see if we can find a handler that
            // handles a supertype of the consumed message.
            foreach (var handlerType in handlers.Keys.Where(type => type.IsAssignableFrom(messageType)))
            {
                return (Func<IMessage<T>, MessageReceivedInfo, Task>)handlers[handlerType];
            }

            if (ThrowOnNoMatchingHandler)
            {
                logger.ErrorWrite("No handler found for message type {0}", messageType.Name);
                throw new EasyNetQException("No handler found for message type {0}", messageType.Name);
            }

            return (message, info) => Task.Factory.StartNew(() => { });
        }

        public dynamic GetHandler(Type messageType)
        {
            Preconditions.CheckNotNull(messageType, "messageType");

            var getHandlerGenericMethod = GetType().GetMethod("GetHandler", new Type[0]).MakeGenericMethod(messageType);

            return getHandlerGenericMethod.Invoke(this, new object[0]);
        }

        public bool ThrowOnNoMatchingHandler { get; set; }
    }
}