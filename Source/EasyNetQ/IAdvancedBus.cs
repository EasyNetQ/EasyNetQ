using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.DI;
using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
/// IAdvancedBus is a lower level API than IBus which gives you fined grained control
/// of routing topology, but keeping the EasyNetQ serialization, persistent connection,
/// error handling and subscription thread.
/// </summary>
public interface IAdvancedBus : IDisposable
{
    /// <summary>
    /// True if a connection of the given type is established.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Establish a connection of the given type.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The IoC container that EasyNetQ uses to resolve its services.
    /// </summary>
    IServiceResolver Container { get; }

    /// <summary>
    /// The conventions used by EasyNetQ to name its routing topology elements.
    /// </summary>
    IConventions Conventions { get; }

    /// <summary>
    /// Consume a stream of messages. Dispatch them to the given handlers
    /// </summary>
    /// <param name="configure">
    /// Fluent configuration e.g. x => x.WithPriority(10)</param>
    /// <returns>A disposable to cancel the consumer</returns>
    IDisposable Consume(Action<IConsumeConfiguration> configure);

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
        Exchange exchange,
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
        Exchange exchange,
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
    /// <param name="properties">The message properties</param>
    /// <param name="body">The message body</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task PublishAsync(
        Exchange exchange,
        string routingKey,
        bool mandatory,
        MessageProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="queue">The name of the queue</param>
    /// <param name="configure">Delegate to configure queue declaration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The queue</returns>
    Task<Queue> QueueDeclareAsync(
        string queue,
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
    Task<Queue> QueueDeclareAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Declare a queue passively. Throw an exception rather than create the queue if it doesn't exist
    /// </summary>
    /// <param name="queue">The queue to declare</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a queue
    /// </summary>
    /// <param name="queue">The name of the queue to delete</param>
    /// <param name="ifUnused">Only delete if unused</param>
    /// <param name="ifEmpty">Only delete if empty</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task QueueDeleteAsync(
        string queue,
        bool ifUnused = false,
        bool ifEmpty = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Purges a queue
    /// </summary>
    /// <param name="queue">The name of the queue to purge</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task QueuePurgeAsync(string queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Declare a exchange passively. Throw an exception rather than create the exchange if it doesn't exist
    /// </summary>
    /// <param name="exchange">The exchange to declare</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Declare an exchange
    /// </summary>
    /// <param name="exchange">The exchange name</param>
    /// <param name="configure">Delegate to configure exchange declaration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The exchange</returns>
    Task<Exchange> ExchangeDeclareAsync(
        string exchange,
        Action<IExchangeDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Delete an exchange
    /// </summary>
    /// <param name="exchange">The exchange to delete</param>
    /// <param name="ifUnused">If set, the server will only delete the exchange if it has no queue bindings.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task ExchangeDeleteAsync(
        string exchange,
        bool ifUnused = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Bind a queue to an exchange.
    /// </summary>
    Task QueueBindAsync(
        string queue,
        string exchange,
        string routingKey,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Unbind a queue from an exchange.
    /// </summary>
    Task QueueUnbindAsync(
        string queue,
        string exchange,
        string routingKey,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Bind an exchange to an exchange.
    /// </summary>
    Task ExchangeBindAsync(
        string destinationExchange,
        string sourceExchange,
        string routingKey,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Unbind an exchange from an exchange.
    /// </summary>
    Task ExchangeUnbindAsync(
        string destinationExchange,
        string sourceExchange,
        string routingKey,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets stats for the given queue
    /// </summary>
    /// <param name="queue">The name of the queue</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The stats of the queue</returns>
    Task<QueueStats> GetQueueStatsAsync(string queue, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a new pulling consumer
    /// </summary>
    /// <param name="queue">The queue</param>
    /// <param name="autoAck"></param>
    /// <returns></returns>
    IPullingConsumer<PullResult> CreatePullingConsumer(in Queue queue, bool autoAck = true);

    /// <summary>
    ///     Creates a new pulling consumer
    /// </summary>
    /// <param name="queue">The queue</param>
    /// <param name="autoAck"></param>
    /// <returns></returns>
    IPullingConsumer<PullResult<T>> CreatePullingConsumer<T>(in Queue queue, bool autoAck = true);

    /// <summary>
    /// Event fires when the bus has connected to a RabbitMQ broker.
    /// </summary>
    event EventHandler<ConnectedEventArgs> Connected;

    /// <summary>
    /// Event fires when the bus has disconnected from a RabbitMQ broker.
    /// </summary>
    event EventHandler<DisconnectedEventArgs> Disconnected;

    /// <summary>
    /// Event fires when the bus gets blocked due to the broker running low on resources.
    /// </summary>
    event EventHandler<BlockedEventArgs> Blocked;

    /// <summary>
    /// Event fires when the bus is unblocked.
    /// </summary>
    event EventHandler<UnblockedEventArgs> Unblocked;

    /// <summary>
    /// Event fires when a mandatory or immediate message is returned as un-routable
    /// </summary>
    event EventHandler<MessageReturnedEventArgs> MessageReturned;
}
