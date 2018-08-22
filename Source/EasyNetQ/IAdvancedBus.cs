using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Topology;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    /// <summary>
    /// IAdvancedBus is a lower level API than IBus which gives you fined grained control
    /// of routing topology, but keeping the EasyNetQ serialization, persistent connection,
    /// error handling and subscription thread.
    /// </summary>
    public interface IAdvancedBus : IDisposable
    {
        /// <summary>
        /// Consume a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queueConsumerPairs">Multiple queue - consumer pairs</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IEnumerable<QueueConsumerPair> queueConsumerPairs, Action<IConsumerConfiguration> configure);

        /// <summary>
        /// Consume a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage) where T : class;

        /// <summary>
        /// Consume a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage, Action<IConsumerConfiguration> configure) where T : class;

        /// <summary>
        /// Consume a stream of messages asynchronously
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage) where T : class;

        /// <summary>
        /// Consume a stream of messages asynchronously
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class;

        /// <summary>
        /// Consume a stream of messages. Dispatch them to the given handlers
        /// </summary>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers to the consumer</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers);

        /// <summary>
        /// Consume a stream of messages. Dispatch them to the given handlers
        /// </summary>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers to the consumer</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers, Action<IConsumerConfiguration> configure);

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="onMessage">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context.
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Action<byte[], MessageProperties, MessageReceivedInfo> onMessage);

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="onMessage">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Action<byte[], MessageProperties, MessageReceivedInfo> onMessage, Action<IConsumerConfiguration> configure);

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="onMessage">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context. Returns a Task.
        /// </param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage);

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="onMessage">
        /// The message handler. Takes the message body, message properties and some information about the
        /// receive context. Returns a Task.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure);

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
        /// <param name="messageProperties">The message properties</param>
        /// <param name="body">The message body</param>
        void Publish(
            IExchange exchange,
            string routingKey,
            bool mandatory,
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
        /// <param name="message">The message to publish</param>
        void Publish<T>(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage<T> message) where T : class;

        /// <summary>
        /// Publish a message as a .NET type when the type is only known at runtime.
        /// Use the generic version of this method <see cref="PublishAsync{T}"/> when you know the type of the message at compile time.
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
        /// <param name="message">The message to publish</param>
        Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage message);

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
        /// <param name="message">The message to publish</param>
        Task PublishAsync<T>(
            IExchange exchange,
            string routingKey,
            bool mandatory,
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
        /// <param name="messageProperties">The message properties</param>
        /// <param name="body">The message body</param>
        Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            MessageProperties messageProperties,
            byte[] body);

        /// <summary>
        /// Declare a queue. If the queue already exists this method does nothing
        /// </summary>
        /// <param name="name">The name of the queue</param>
        /// <param name="passive">Throw an exception rather than create the queue if it doesn't exist</param>
        /// <param name="durable">Durable queues remain active when a server restarts.</param>
        /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.</param>
        /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
        /// <param name="perQueueMessageTtl">Determines how long a message published to a queue can live before it is discarded by the server.</param>
        /// <param name="expires">Determines how long a queue can remain unused before it is automatically deleted by the server.</param>
        /// <param name="maxPriority">Determines the maximum message priority that the queue should support.</param>
        /// <param name="deadLetterExchange">Determines an exchange's name can remain unused before it is automatically deleted by the server.</param>
        /// <param name="deadLetterRoutingKey">If set, will route message with the routing key specified, if not set, message will be routed with the same routing keys they were originally published with.</param>
        /// <param name="maxLength">The maximum number of ready messages that may exist on the queue.  Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached</param>
        /// <param name="maxLengthBytes">The maximum size of the queue in bytes.  Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached</param>
        /// <returns>
        /// The queue
        /// </returns>
        IQueue QueueDeclare(
            string name,
            bool passive = false,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            int? perQueueMessageTtl  = null,
            int? expires = null,
            int? maxPriority = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null,
            int? maxLength = null,
            int? maxLengthBytes = null);

        /// <summary>
        /// Declare a queue. If the queue already exists this method does nothing
        /// </summary>
        /// <param name="name">The name of the queue</param>
        /// <param name="passive">Throw an exception rather than create the queue if it doesn't exist</param>
        /// <param name="durable">Durable queues remain active when a server restarts.</param>
        /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.</param>
        /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
        /// <param name="perQueueMessageTtl">Determines how long a message published to a queue can live before it is discarded by the server.</param>
        /// <param name="expires">Determines how long a queue can remain unused before it is automatically deleted by the server.</param>
        /// <param name="maxPriority">Determines the maximum message priority that the queue should support.</param>
        /// <param name="deadLetterExchange">Determines an exchange's name can remain unused before it is automatically deleted by the server.</param>
        /// <param name="deadLetterRoutingKey">If set, will route message with the routing key specified, if not set, message will be routed with the same routing keys they were originally published with.</param>
        /// <param name="maxLength">The maximum number of ready messages that may exist on the queue.  Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached</param>
        /// <param name="maxLengthBytes">The maximum size of the queue in bytes.  Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached</param>
        /// <returns>The queue</returns>
        Task<IQueue> QueueDeclareAsync(
            string name,
            bool passive = false,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            int? perQueueMessageTtl  = null,
            int? expires = null,
            int? maxPriority = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null,
            int? maxLength = null,
            int? maxLengthBytes = null);

        /// <summary>
        /// Delete a queue
        /// </summary>
        /// <param name="queue">The queue to delete</param>
        /// <param name="ifUnused">Only delete if unused</param>
        /// <param name="ifEmpty">Only delete if empty</param>
        void QueueDelete(IQueue queue, bool ifUnused = false, bool ifEmpty = false);

        /// <summary>
        /// Purges a queue
        /// </summary>
        /// <param name="queue">The queue to purge</param>
        void QueuePurge(IQueue queue);

        /// <summary>
        /// Declare an exchange
        /// </summary>
        /// <param name="name">The exchange name</param>
        /// <param name="type">The type of exchange</param>
        /// <param name="passive">Throw an exception rather than create the exchange if it doesn't exist</param>
        /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
        /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
        /// <param name="internal">If set, the exchange may not be used directly by publishers, but only when bound to other exchanges.</param>
        /// <param name="alternateExchange">Route messages to this exchange if they cannot be routed.</param>
        /// <param name="delayed">If set, declares x-delayed-type exchange for routing delayed messages.</param>
        /// <returns>The exchange</returns>
        IExchange ExchangeDeclare(
            string name,
            string type,
            bool passive = false,
            bool durable = true,
            bool autoDelete = false,
            bool @internal = false,
            string alternateExchange = null,
            bool delayed = false);

        /// <summary>
        /// Declare an exchange
        /// </summary>
        /// <param name="name">The exchange name</param>
        /// <param name="type">The type of exchange</param>
        /// <param name="passive">Throw an exception rather than create the exchange if it doesn't exist</param>
        /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
        /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
        /// <param name="internal">If set, the exchange may not be used directly by publishers, but only when bound to other exchanges.</param>
        /// <param name="alternateExchange">Route messages to this exchange if they cannot be routed.</param>
        /// <param name="delayed">If set, declares x-delayed-type exchange for routing delayed messages.</param>
        /// <returns>The exchange</returns>
        Task<IExchange> ExchangeDeclareAsync(
            string name,
            string type,
            bool passive = false,
            bool durable = true,
            bool autoDelete = false,
            bool @internal = false,
            string alternateExchange = null,
            bool delayed = false);

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
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <returns>A binding</returns>
        IBinding Bind(IExchange exchange, IQueue queue, string routingKey, IDictionary<string, object> headers);

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey);

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <returns>A binding</returns>
        Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey, IDictionary<string, object> headers);

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        IBinding Bind(IExchange source, IExchange destination, string routingKey);

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <returns>A binding</returns>
        IBinding Bind(IExchange source, IExchange destination, string routingKey, IDictionary<string, object> headers);

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey);

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <returns>A binding</returns>
        Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey, IDictionary<string, object> headers);

        /// <summary>
        /// Delete a binding
        /// </summary>
        /// <param name="binding">the binding to delete</param>
        void BindingDelete(IBinding binding);

        /// <summary>
        /// Get a message from the given queue.
        /// </summary>
        /// <typeparam name="T">The message type to get</typeparam>
        /// <param name="queue">The queue from which to retrieve the message</param>
        /// <returns>An IBasicGetResult.</returns>
        IBasicGetResult<T> Get<T>(IQueue queue) where T : class;

        /// <summary>
        /// Get the raw message from the given queue.
        /// </summary>
        /// <param name="queue">The queue from which to retrieve the message</param>
        /// <returns>An IBasicGetResult</returns>
        IBasicGetResult Get(IQueue queue);

        /// <summary>
        /// Counts messages in the given queue
        /// </summary>
        /// <param name="queue">The queue in which to count messages</param>
        /// <returns>The number of counted messages</returns>
        uint MessageCount(IQueue queue);

        /// <summary>
        /// True if the bus is connected, False if it is not.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Event fires when the bus has connected to a RabbitMQ broker.
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        /// Event fires when the bus has disconnected from a RabbitMQ broker.
        /// </summary>
        event EventHandler Disconnected;

        /// <summary>
        /// Event fires when the bus gets blocked due to the broker running low on resources.
        /// </summary>
        event EventHandler<ConnectionBlockedEventArgs> Blocked;

        /// <summary>
        /// Event fires when the bus is unblocked.
        /// </summary>
        event EventHandler Unblocked;

        /// <summary>
        /// Event fires when a mandatory or immediate message is returned as un-routable
        /// </summary>
        event EventHandler<MessageReturnedEventArgs> MessageReturned;

        /// <summary>
        /// The IoC container that EasyNetQ uses to resolve its services.
        /// </summary>
        IServiceResolver Container { get; }

        /// <summary>
        /// The conventions used by EasyNetQ to name its routing topology elements.
        /// </summary>
        IConventions Conventions { get; }
        
        /// <summary>
        /// Declare a transient server named queue. Note, this queue will only last for duration of the
        /// connection. If there is a connection outage, EasyNetQ will not attempt to recreate
        /// consumers.
        /// </summary>
        /// <returns>The queue</returns>
        IQueue QueueDeclare();

        /// <summary>
        /// Declare a transient server named queue. Note, this queue will only last for duration of the
        /// connection. If there is a connection outage, EasyNetQ will not attempt to recreate
        /// consumers.
        /// </summary>
        /// <returns>The queue</returns>
        Task<IQueue> QueueDeclareAsync();
    }
}