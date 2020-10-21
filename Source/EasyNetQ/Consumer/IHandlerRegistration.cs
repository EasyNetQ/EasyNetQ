using System;

namespace EasyNetQ.Consumer
{
    public interface IHandlerRegistration
    {
        /// <summary>
        /// Add an asynchronous handler
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="handler">The handler</param>
        /// <returns></returns>
        IHandlerRegistration Add<T>(IMessageHandler<T> handler);

        /// <summary>
        /// Set to true if the handler collection should throw an EasyNetQException when no
        /// matching handler is found, or false if it should return a noop handler.
        /// Default is true.
        /// </summary>
        bool ThrowOnNoMatchingHandler { get; set; }
    }

    /// <inheritdoc />
    public interface IHandlerCollection : IHandlerRegistration
    {
        /// <summary>
        /// Retrieve a handler from the collection.
        /// If a matching handler cannot be found, the handler collection will either throw
        /// an EasyNetQException, or return null, depending on the value of the
        /// ThrowOnNoMatchingHandler property.
        /// </summary>
        /// <typeparam name="T">The type of handler to return</typeparam>
        /// <returns>The handler</returns>
        IMessageHandler<T> GetHandler<T>();

        /// <summary>
        /// Retrieve a handler from the collection.
        /// If a matching handler cannot be found, the handler collection will either throw
        /// an EasyNetQException, or return null, depending on the value of the
        /// ThrowOnNoMatchingHandler property.
        /// </summary>
        /// <param name="messageType">The type of handler to return</param>
        /// <returns>The handler</returns>
        IMessageHandler GetHandler(Type messageType);
    }
}
