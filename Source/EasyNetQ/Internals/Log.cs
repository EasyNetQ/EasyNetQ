using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Internals;

/// <summary>
///     Source-generated logging extension methods for EasyNetQ.
///     EventId ranges: 100-199 connection, 200-299 channel, 300-399 consumer,
///     400-499 producer/advanced bus, 500-599 rpc, 600-699 error strategy, 700-799 infrastructure.
/// </summary>
internal static partial class Log
{
    #region Connection (100-199)

    [LoggerMessage(100, LogLevel.Information, "Connection {Type} established to broker {Broker}, port {Port}")]
    public static partial void ConnectionEstablished(this ILogger logger, PersistentConnectionType type, string broker, int port);

    [LoggerMessage(101, LogLevel.Information, "Connection {Type} recovered to broker {Host}:{Port}")]
    public static partial void ConnectionRecovered(this ILogger logger, PersistentConnectionType type, string host, int port);

    [LoggerMessage(102, LogLevel.Debug, "Connection {Type} disconnected from broker {Host}:{Port} because of {Reason}")]
    public static partial void ConnectionDisconnected(this ILogger logger, Exception? exception, PersistentConnectionType type, string host, int port, string? reason);

    [LoggerMessage(103, LogLevel.Information, "Connection {Type} blocked with reason {Reason}")]
    public static partial void ConnectionBlocked(this ILogger logger, PersistentConnectionType type, string? reason);

    [LoggerMessage(104, LogLevel.Information, "Connection {Type} unblocked")]
    public static partial void ConnectionUnblocked(this ILogger logger, PersistentConnectionType type);

    #endregion

    #region Channel (200-299)

    [LoggerMessage(200, LogLevel.Error, "Failed to fast invoke channel action, invocation will be retried")]
    public static partial void FailedToFastInvokeChannelAction(this ILogger logger, Exception exception);

    [LoggerMessage(201, LogLevel.Warning, "Semaphore was already disposed during channel release!")]
    public static partial void SemaphoreAlreadyDisposed(this ILogger logger, Exception exception);

    [LoggerMessage(202, LogLevel.Error, "Failed to invoke channel action, invocation will be retried")]
    public static partial void FailedToInvokeChannelAction(this ILogger logger, Exception exception);

    #endregion

    #region Consumer (300-399)

    [LoggerMessage(300, LogLevel.Information, "Channel has shutdown with soft error and will be recreated")]
    public static partial void ChannelShutdownWithSoftError(this ILogger logger);

    [LoggerMessage(301, LogLevel.Error, "Failed to create channel")]
    public static partial void FailedToCreateChannel(this ILogger logger, Exception exception);

    [LoggerMessage(302, LogLevel.Information, "Declared consumer with consumerTag {ConsumerTag} on queue {Queue}")]
    public static partial void ConsumerDeclared(this ILogger logger, string consumerTag, string queue);

    [LoggerMessage(303, LogLevel.Error, "Failed to declare consumer on queue {Queue}")]
    public static partial void FailedToDeclareConsumer(this ILogger logger, Exception exception, string queue);

    [LoggerMessage(304, LogLevel.Error, "Failed to stop consuming on consumerTag {ConsumerTag}")]
    public static partial void FailedToStopConsuming(this ILogger logger, Exception exception, string consumerTag);

    [LoggerMessage(305, LogLevel.Error, "Failed to dispose on consumerTag {ConsumerTag}")]
    public static partial void FailedToDisposeConsumer(this ILogger logger, Exception exception, string consumerTag);

    [LoggerMessage(306, LogLevel.Information, "Consumer with consumerTags {ConsumerTags} has cancelled")]
    public static partial void ConsumerCancelled(this ILogger logger, string consumerTags);

    [LoggerMessage(307, LogLevel.Information, "Failed to ACK or NACK, message will be retried, consumerTag={ConsumerTag}, deliveryTag={DeliveryTag}, queue={Queue}")]
    public static partial void FailedToAckOrNack(this ILogger logger, Exception exception, string consumerTag, ulong deliveryTag, string queue);

    [LoggerMessage(308, LogLevel.Error, "Unexpected exception when attempting to ACK or NACK, consumerTag={ConsumerTag}, deliveryTag={DeliveryTag}, queue={Queue}")]
    public static partial void UnexpectedExceptionOnAckOrNack(this ILogger logger, Exception exception, string consumerTag, ulong deliveryTag, string queue);

    #endregion

    #region Producer / advanced bus (400-499)

    [LoggerMessage(400, LogLevel.Debug, "{Queue} has {MessagesCount} messages and {ConsumersCount} consumers.")]
    public static partial void QueueStatsRetrieved(this ILogger logger, string queue, uint messagesCount, uint consumersCount);

    [LoggerMessage(401, LogLevel.Debug, "Passive declared queue {Queue}")]
    public static partial void QueueDeclaredPassive(this ILogger logger, string queue);

    [LoggerMessage(402, LogLevel.Debug, "Declared queue {Queue}: durable={Durable}, exclusive={Exclusive}, autoDelete={AutoDelete}, arguments={Arguments}")]
    public static partial void QueueDeclared(this ILogger logger, string queue, bool durable, bool exclusive, bool autoDelete, string? arguments);

    [LoggerMessage(403, LogLevel.Debug, "Deleted queue {Queue}")]
    public static partial void QueueDeleted(this ILogger logger, string queue);

    [LoggerMessage(404, LogLevel.Debug, "Purged queue {Queue}")]
    public static partial void QueuePurged(this ILogger logger, string queue);

    [LoggerMessage(405, LogLevel.Debug, "Passive declared exchange {Exchange}")]
    public static partial void ExchangeDeclaredPassive(this ILogger logger, string exchange);

    [LoggerMessage(406, LogLevel.Debug, "Declared exchange {Exchange}: type={Type}, durable={Durable}, autoDelete={AutoDelete}, arguments={Arguments}")]
    public static partial void ExchangeDeclared(this ILogger logger, string exchange, string type, bool durable, bool autoDelete, string? arguments);

    [LoggerMessage(407, LogLevel.Debug, "Deleted exchange {Exchange}")]
    public static partial void ExchangeDeleted(this ILogger logger, string exchange);

    [LoggerMessage(408, LogLevel.Debug, "Bound queue {Queue} to exchange {Exchange} with routing key {RoutingKey} and arguments {Arguments}")]
    public static partial void QueueBound(this ILogger logger, string queue, string exchange, string routingKey, string? arguments);

    [LoggerMessage(409, LogLevel.Debug, "Unbound queue {Queue} from exchange {Exchange} with routing key {RoutingKey} and arguments {Arguments}")]
    public static partial void QueueUnbound(this ILogger logger, string queue, string exchange, string routingKey, string? arguments);

    [LoggerMessage(410, LogLevel.Debug, "Bound destination exchange {DestinationExchange} to source exchange {SourceExchange} with routing key {RoutingKey} and arguments {Arguments}")]
    public static partial void ExchangeBound(this ILogger logger, string destinationExchange, string sourceExchange, string routingKey, string? arguments);

    [LoggerMessage(411, LogLevel.Debug, "Unbound destination exchange {DestinationExchange} from source exchange {SourceExchange} with routing key {RoutingKey} and arguments {Arguments}")]
    public static partial void ExchangeUnbound(this ILogger logger, string destinationExchange, string sourceExchange, string routingKey, string? arguments);

    #endregion

    #region Rpc (500-599)

    [LoggerMessage(500, LogLevel.Debug, "Subscribing for {RequestType}/{ResponseType}")]
    public static partial void RpcSubscribing(this ILogger logger, Type requestType, Type responseType);

    [LoggerMessage(501, LogLevel.Debug, "Subscription for {RequestType}/{ResponseType} is created")]
    public static partial void RpcSubscriptionCreated(this ILogger logger, Type requestType, Type responseType);

    #endregion

    #region Error strategy (600-699)

    [LoggerMessage(600, LogLevel.Error, "Exception thrown by subscription callback, queue={Queue}, routingKey={RoutingKey}, exchange={Exchange}, correlationId={CorrelationId}")]
    public static partial void ConsumeCallbackFailed(this ILogger logger, Exception exception, string queue, string routingKey, string exchange, string? correlationId);

    [LoggerMessage(601, LogLevel.Error, "Body of message that failed in subscription callback, queue={Queue}, body={Body}")]
    public static partial void FailedMessageBody(this ILogger logger, string queue, string body);

    [LoggerMessage(602, LogLevel.Error, "Cannot connect to broker while attempting to publish error message")]
    public static partial void CannotConnectToBrokerForErrorPublish(this ILogger logger, Exception exception);

    [LoggerMessage(603, LogLevel.Error, "Broker connection was closed while attempting to publish error message")]
    public static partial void BrokerConnectionClosedForErrorPublish(this ILogger logger, Exception exception);

    [LoggerMessage(604, LogLevel.Error, "Failed to publish error message")]
    public static partial void FailedToPublishErrorMessage(this ILogger logger, Exception exception);

    [LoggerMessage(605, LogLevel.Error, "Consume error strategy has failed")]
    public static partial void ConsumeErrorStrategyFailed(this ILogger logger, Exception exception);

    #endregion

    #region Infrastructure (700-799)

    [LoggerMessage(700, LogLevel.Error, "Error from timer callback")]
    public static partial void TimerCallbackError(this ILogger logger, Exception exception);

    [LoggerMessage(701, LogLevel.Error, "Failed to handle {Event}")]
    public static partial void FailedToHandleEvent(this ILogger logger, Exception exception, string? @event);

    #endregion
}
