using System;
using System.Collections.Generic;
using System.Threading;
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
        /// True if the bus is connected, False if it is not.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The IoC container that EasyNetQ uses to resolve its services.
        /// </summary>
        IServiceResolver Container { get; }

        /// <summary>
        /// The conventions used by EasyNetQ to name its routing topology elements.
        /// </summary>
        IConventions Conventions { get; }

        /// <summary>
        /// Consume a stream of messages
        /// </summary>
        /// <param name="queueConsumerPairs">Multiple queue - consumer pairs</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IEnumerable<QueueConsumerPair> queueConsumerPairs, Action<IConsumerConfiguration> configure);

        /// <summary>
        /// Consume a stream of messages asynchronously
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> onMessage, Action<IConsumerConfiguration> configure);

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
        /// receive context. Returns a Task.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithPriority(10)</param>
        /// <returns>A disposable to cancel the consumer</returns>
        IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage, Action<IConsumerConfiguration> configure);

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
        /// <param name="cancellationToken">The cancellation token</param>
        Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage message,
            CancellationToken cancellationToken = default
        );

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
        /// <param name="cancellationToken">The cancellation token</param>
        Task PublishAsync<T>(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage<T> message,
            CancellationToken cancellationToken = default
        );

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
        /// <param name="cancellationToken">The cancellation token</param>
        Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            MessageProperties messageProperties,
            byte[] body,
            CancellationToken cancellationToken = default
        );

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
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The queue</returns>
        Task<IQueue> QueueDeclareAsync(
            string name,
            Action<IQueueDeclareConfiguration> configure,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Declare a transient server named queue. Note, this queue will only last for duration of the
        /// connection. If there is a connection outage, EasyNetQ will not attempt to recreate
        /// consumers.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The queue</returns>
        Task<IQueue> QueueDeclareAsync(CancellationToken cancellationToken = default);


        /// <summary>
        /// Declare a queue passively. Throw an exception rather than create the queue if it doesn't exist
        /// </summary>
        /// <param name="name">The queue to declare</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task QueueDeclarePassiveAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a queue
        /// </summary>
        /// <param name="queue">The queue to delete</param>
        /// <param name="ifUnused">Only delete if unused</param>
        /// <param name="ifEmpty">Only delete if empty</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task QueueDeleteAsync(IQueue queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Purges a queue
        /// </summary>
        /// <param name="queue">The queue to purge</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task QueuePurgeAsync(IQueue queue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Declare an exchange
        /// </summary>
        /// <param name="name">The exchange name</param>
        /// <param name="type">The type of exchange</param>
        /// <param name="passive">Throw an exception rather than create the exchange if it doesn't exist</param>
        /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
        /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
        /// <param name="alternateExchange">Route messages to this exchange if they cannot be routed.</param>
        /// <param name="delayed">If set, declares x-delayed-type exchange for routing delayed messages.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The exchange</returns>
        Task<IExchange> ExchangeDeclareAsync(
            string name,
            string type,
            bool passive = false,
            bool durable = true,
            bool autoDelete = false,
            string alternateExchange = null,
            bool delayed = false,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Delete an exchange
        /// </summary>
        /// <param name="exchange">The exchange to delete</param>
        /// <param name="ifUnused">If set, the server will only delete the exchange if it has no queue bindings.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task ExchangeDeleteAsync(IExchange exchange, bool ifUnused = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A binding</returns>
        Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey, IDictionary<string, object> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a binding
        /// </summary>
        /// <param name="binding">the binding to delete</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task UnbindAsync(IBinding binding, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a message from the given queue.
        /// </summary>
        /// <typeparam name="T">The message type to get</typeparam>
        /// <param name="queue">The queue from which to retrieve the message</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>An IBasicGetResult.</returns>
        Task<IBasicGetResult<T>> GetMessageAsync<T>(IQueue queue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the raw message from the given queue.
        /// </summary>
        /// <param name="queue">The queue from which to retrieve the message</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>An IBasicGetResult</returns>
        Task<IBasicGetResult> GetMessageAsync(IQueue queue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts messages in the given queue
        /// </summary>
        /// <param name="queue">The queue in which to count messages</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The number of counted messages</returns>
        Task<uint> GetMessagesCountAsync(IQueue queue, CancellationToken cancellationToken = default);

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
    }
}
