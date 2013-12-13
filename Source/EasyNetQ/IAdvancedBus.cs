﻿using System;
using System.Collections;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    /// IAdvancedBus is a lower level API than IBus which gives you fined grained control
    /// of routing topology, but keeping the EasyNetQ serialisation, persistent connection,
    /// error handling and subscription thread.
    /// </summary>
    public interface IAdvancedBus : IDisposable
    {
        /// <summary>
        /// Consume a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage) where T : class;

        /// <summary>
        /// Consume a stream of messages asynchronously
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage) where T : class;

        /// <summary>
        /// Consume a stream of messages. Dispatch them to the given handlers
        /// </summary>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers to the consumer</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers);

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="onMessage">
        /// The message handler. Takes the message body, message properties and some information about the 
        /// receive context. Returns a Task.
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage);

        /// <summary>
        /// Publish a message as a byte array
        /// </summary>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">
        /// The routing key for the message. The routing key is used for routing messages depending on the 
        /// exchange configuration.</param>
        /// <param name="mandatory">
        /// This flag tells the server how to react if the message cannot be routed to a queue. 
        /// If this flag is true, the server will return an unroutable message with a Return method. 
        /// If this flag is false, the server silently drops the message.
        /// </param>
        /// <param name="immediate">
        /// This flag tells the server how to react if the message cannot be routed to a queue consumer immediately. 
        /// If this flag is true, the server will return an undeliverable message with a Return method. 
        /// If this flag is false, the server will queue the message, but with no guarantee that it will ever be consumed.
        /// </param>
        /// <param name="messageProperties">The message properties</param>
        /// <param name="body">The message body</param>
        void Publish(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            bool immediate,
            MessageProperties messageProperties,
            byte[] body);

        /// <summary>
        /// Publish a message as a .NET type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">
        /// The routing key for the message. The routing key is used for routing messages depending on the 
        /// exchange configuration.</param>
        /// <param name="mandatory">
        /// This flag tells the server how to react if the message cannot be routed to a queue. 
        /// If this flag is true, the server will return an unroutable message with a Return method. 
        /// If this flag is false, the server silently drops the message.
        /// </param>
        /// <param name="immediate">
        /// This flag tells the server how to react if the message cannot be routed to a queue consumer immediately. 
        /// If this flag is true, the server will return an undeliverable message with a Return method. 
        /// If this flag is false, the server will queue the message, but with no guarantee that it will ever be consumed.
        /// </param>
        /// <param name="message">The message to publish</param>
        void Publish<T>(
            IExchange exchange, 
            string routingKey,
            bool mandatory,
            bool immediate,
            IMessage<T> message) where T : class;

        /// <summary>
        /// Publish a message as a byte array.
        /// Task completes after publish has completed. If publisherConfirms=true is set in the connection string,
        /// the task completes after an ACK is received. The task will throw on either NACK or timeout.
        /// </summary>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">
        /// The routing key for the message. The routing key is used for routing messages depending on the 
        /// exchange configuration.</param>
        /// <param name="mandatory">
        /// This flag tells the server how to react if the message cannot be routed to a queue. 
        /// If this flag is true, the server will return an unroutable message with a Return method. 
        /// If this flag is false, the server silently drops the message.
        /// </param>
        /// <param name="immediate">
        /// This flag tells the server how to react if the message cannot be routed to a queue consumer immediately. 
        /// If this flag is true, the server will return an undeliverable message with a Return method. 
        /// If this flag is false, the server will queue the message, but with no guarantee that it will ever be consumed.
        /// </param>
        /// <param name="messageProperties">The message properties</param>
        /// <param name="body">The message body</param>
        Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            bool immediate,
            MessageProperties messageProperties,
            byte[] body);

        /// <summary>
        /// Publish a message as a .NET type
        /// Task completes after publish has completed. If publisherConfirms=true is set in the connection string,
        /// the task completes after an ACK is received. The task will throw on either NACK or timeout.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">
        /// The routing key for the message. The routing key is used for routing messages depending on the 
        /// exchange configuration.</param>
        /// <param name="mandatory">
        /// This flag tells the server how to react if the message cannot be routed to a queue. 
        /// If this flag is true, the server will return an unroutable message with a Return method. 
        /// If this flag is false, the server silently drops the message.
        /// </param>
        /// <param name="immediate">
        /// This flag tells the server how to react if the message cannot be routed to a queue consumer immediately. 
        /// If this flag is true, the server will return an undeliverable message with a Return method. 
        /// If this flag is false, the server will queue the message, but with no guarantee that it will ever be consumed.
        /// </param>
        /// <param name="message">The message to publish</param>
        Task PublishAsync<T>(
            IExchange exchange, 
            string routingKey,
            bool mandatory,
            bool immediate,
            IMessage<T> message) where T : class;



        /// <summary>
        /// Declare a queue. If the queue already exists this method does nothing
        /// </summary>
        /// <param name="name">The name of the queue</param>
        /// <param name="passive">Throw an exception rather than create the queue if it doesn't exist</param>
        /// <param name="durable">Durable queues remain active when a server restarts.</param>
        /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, 
        ///     and are deleted when that connection closes.</param>
        /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
        /// <param name="perQueueTtl">How long a message published to a queue can live before it is discarded by the server.</param>
        /// <param name="expires">Determines how long a queue can remain unused before it is automatically deleted by the server.</param>
        /// <returns>The queue</returns>
        IQueue QueueDeclare(string name, bool passive = false, bool durable = true, bool exclusive = false, bool autoDelete = false, int perQueueTtl = int.MaxValue, int expires = int.MaxValue);

        /// <summary>
        /// Declare a transient server named queue. Note, this queue will only last for duration of the
        /// connection. If there is a connection outage, EasyNetQ will not attempt to recreate
        /// consumers.
        /// </summary>
        /// <returns>The queue</returns>
        IQueue QueueDeclare();

        /// <summary>
        /// Delete a queue
        /// </summary>
        /// <param name="queue">The queue to delete</param>
        /// <param name="ifUnused">Only delete if unused</param>
        /// <param name="ifEmpty">Only delete if empty</param>
        void QueueDelete(IQueue queue, bool ifUnused = false, bool ifEmpty = false);

        /// <summary>
        /// Purget a queue
        /// </summary>
        /// <param name="queue">The queue to purge</param>
        void QueuePurge(IQueue queue);

        /// <summary>
        /// Declare an exchange
        /// </summary>
        /// <param name="name">The exchange name</param>
        /// <param name="type">The type of exchange</param>
        /// <param name="passive">Throw an exception rather than create the exchange if it doens't exist</param>
        /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
        /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
        /// <param name="internal">If set, the exchange may not be used directly by publishers, 
        ///     but only when bound to other exchanges.</param>
        /// <param name="arguments">If set, extra parameters for the exchange</param>
        /// <returns>The exchange</returns>
        IExchange ExchangeDeclare(string name, string type, bool passive = false, bool durable = true, bool autoDelete = false, bool @internal = false, IDictionary arguments=null);

        /// <summary>
        /// Delete an exchange
        /// </summary>
        /// <param name="exchange">The exchange to delete</param>
        /// <param name="ifUnused">If set, the server will only delete the exchange if it has no queue bindings.</param>
        void ExchangeDelete(IExchange exchange, bool ifUnused = false);

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        IBinding Bind(IExchange exchange, IQueue queue, string routingKey);

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        IBinding Bind(IExchange source, IExchange destination, string routingKey);

        /// <summary>
        /// Delete a binding
        /// </summary>
        /// <param name="binding">the binding to delete</param>
        void BindingDelete(IBinding binding);

        /// <summary>
        /// True if the bus is connected, False if it is not.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Event fires when the bus connects
        /// </summary>
        event Action Connected;

        /// <summary>
        /// Event fires when the bus disconnects
        /// </summary>
        event Action Disconnected;

        /// <summary>
        /// The IoC container that EasyNetQ uses to resolve it's services.
        /// </summary>
        IContainer Container { get; }
    }
}