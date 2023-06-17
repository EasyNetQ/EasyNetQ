using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IAdvancedBus"/>
/// </summary>
public static partial class AdvancedBusExtensions
{
    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
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
    public static Task PublishAsync(
        this IAdvancedBus bus,
        string exchange,
        string routingKey,
        bool? mandatory,
        IMessage message,
        CancellationToken cancellationToken = default
    ) => bus.PublishAsync(exchange, routingKey, mandatory, null, message, cancellationToken);

    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
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
    public static Task PublishAsync(
        IAdvancedBus bus,
        string exchange,
        string routingKey,
        bool? mandatory,
        MessageProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    ) => bus.PublishAsync(exchange, routingKey, mandatory, null, properties, body, cancellationToken);

    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to publish to</param>
    /// <param name="routingKey">
    /// The routing key for the message. The routing key is used for routing messages depending on the
    /// exchange configuration.</param>
    /// <param name="mandatory">
    /// This flag tells the server how to react if the message cannot be routed to a queue.
    /// If this flag is true, the server will return an unroutable message with a Return method.
    /// If this flag is false, the server silently drops the message.
    /// </param>
    /// <param name="publisherConfirms">
    /// Overrides ConnectionConfiguration.PublisherConfirms per request.
    /// If this flag is true the task completes after an ACK is received.
    /// If this flag is null value from ConnectionConfiguration.PublisherConfirms will be used.
    /// </param>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task PublishAsync(
        this IAdvancedBus bus,
        in Exchange exchange,
        string routingKey,
        bool? mandatory,
        bool? publisherConfirms,
        IMessage message,
        CancellationToken cancellationToken = default
    ) => bus.PublishAsync(exchange.Name, routingKey, mandatory, publisherConfirms, message, cancellationToken);

    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
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
    public static Task PublishAsync(
        this IAdvancedBus bus,
        in Exchange exchange,
        string routingKey,
        bool? mandatory,
        in MessageProperties properties,
        in ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    ) => bus.PublishAsync(exchange, routingKey, mandatory, null, properties, body, cancellationToken);

    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to publish to</param>
    /// <param name="routingKey">
    /// The routing key for the message. The routing key is used for routing messages depending on the
    /// exchange configuration.</param>
    /// <param name="mandatory">
    /// This flag tells the server how to react if the message cannot be routed to a queue.
    /// If this flag is true, the server will return an unroutable message with a Return method.
    /// If this flag is false, the server silently drops the message.
    /// </param>
    /// <param name="publisherConfirms">
    /// Overrides ConnectionConfiguration.PublisherConfirms per request.
    /// If this flag is true the task completes after an ACK is received.
    /// If this flag is null value from ConnectionConfiguration.PublisherConfirms will be used.
    /// </param>
    /// <param name="properties">The message properties</param>
    /// <param name="body">The message body</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task PublishAsync(
        this IAdvancedBus bus,
        in Exchange exchange,
        string routingKey,
        bool? mandatory,
        bool? publisherConfirms,
        in MessageProperties properties,
        in ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    ) => bus.PublishAsync(exchange.Name, routingKey, mandatory, publisherConfirms, properties, body, cancellationToken);

    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to publish to</param>
    /// <param name="routingKey">
    /// The routing key for the message. The routing key is used for routing messages depending on the
    /// exchange configuration.
    /// </param>
    /// <param name="mandatory">
    /// This flag tells the server how to react if the message cannot be routed to a queue.
    /// If this flag is true, the server will return an unroutable message with a Return method.
    /// If this flag is false, the server silently drops the message.
    /// </param>
    /// <param name="publisherConfirms">
    /// Overrides ConnectionConfiguration.PublisherConfirms per request.
    /// If this flag is true the task completes after an ACK is received.
    /// If this flag is null value from ConnectionConfiguration.PublisherConfirms will be used.
    /// </param>
    /// <param name="messageProperties">The message properties</param>
    /// <param name="body">The message body</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void Publish(
        this IAdvancedBus bus,
        in Exchange exchange,
        string routingKey,
        bool? mandatory,
        bool? publisherConfirms,
        in MessageProperties messageProperties,
        in ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    )
    {
        bus.PublishAsync(exchange, routingKey, mandatory, publisherConfirms, messageProperties, body, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to publish to</param>
    /// <param name="routingKey">
    /// The routing key for the message. The routing key is used for routing messages depending on the
    /// exchange configuration.
    /// </param>
    /// <param name="mandatory">
    /// This flag tells the server how to react if the message cannot be routed to a queue.
    /// If this flag is true, the server will return an unroutable message with a Return method.
    /// If this flag is false, the server silently drops the message.
    /// </param>
    /// <param name="publisherConfirms">
    /// Overrides ConnectionConfiguration.PublisherConfirms per request.
    /// If this flag is true the task completes after an ACK is received.
    /// If this flag is null value from ConnectionConfiguration.PublisherConfirms will be used.
    /// </param>
    /// <param name="messageProperties">The message properties</param>
    /// <param name="body">The message body</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void Publish(
        this IAdvancedBus bus,
        string exchange,
        string routingKey,
        bool? mandatory,
        bool? publisherConfirms,
        in MessageProperties messageProperties,
        in ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    )
    {
        bus.PublishAsync(exchange, routingKey, mandatory, publisherConfirms, messageProperties, body, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to publish to</param>
    /// <param name="routingKey">
    /// The routing key for the message. The routing key is used for routing messages depending on the
    /// exchange configuration.
    /// </param>
    /// <param name="mandatory">
    /// This flag tells the server how to react if the message cannot be routed to a queue.
    /// If this flag is true, the server will return an unroutable message with a Return method.
    /// If this flag is false, the server silently drops the message.
    /// </param>
    /// <param name="messageProperties">The message properties</param>
    /// <param name="body">The message body</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void Publish(
        this IAdvancedBus bus,
        in Exchange exchange,
        string routingKey,
        bool? mandatory,
        in MessageProperties messageProperties,
        in ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    )
    {
        bus.PublishAsync(exchange, routingKey, mandatory, messageProperties, body, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Publish a message as a .NET type when the type is only known at runtime.
    /// Task completes after publish has completed.
    /// If publisher confirms enabled the task completes after an ACK is received.
    /// The task will throw on either NACK or timeout.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to publish to</param>
    /// <param name="routingKey">
    /// The routing key for the message. The routing key is used for routing messages depending on the
    /// exchange configuration.
    /// </param>
    /// <param name="mandatory">
    /// This flag tells the server how to react if the message cannot be routed to a queue.
    /// If this flag is true, the server will return an unroutable message with a Return method.
    /// If this flag is false, the server silently drops the message.
    /// </param>
    /// <param name="messageProperties">The message properties</param>
    /// <param name="body">The message body</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void Publish(
        this IAdvancedBus bus,
        string exchange,
        string routingKey,
        bool? mandatory,
        in MessageProperties messageProperties,
        in ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    )
    {
        bus.PublishAsync(exchange, routingKey, mandatory, null, messageProperties, body, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }
}
