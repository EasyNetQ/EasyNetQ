namespace EasyNetQ.Diagnostics;

/// <summary>
///     Attribute names used on EasyNetQ spans and metrics, mirroring the OpenTelemetry messaging semantic
///     conventions (Development stability) plus a small <c>easynetq.*</c> namespace
/// </summary>
public static class MessagingTags
{
    /// <summary>messaging.system</summary>
    public const string MessagingSystem = "messaging.system";

    /// <summary>messaging.operation.type</summary>
    public const string OperationType = "messaging.operation.type";

    /// <summary>messaging.operation.name</summary>
    public const string OperationName = "messaging.operation.name";

    /// <summary>messaging.destination.name (the exchange)</summary>
    public const string DestinationName = "messaging.destination.name";

    /// <summary>messaging.destination.subscription.name (the queue)</summary>
    public const string DestinationSubscriptionName = "messaging.destination.subscription.name";

    /// <summary>messaging.rabbitmq.destination.routing_key</summary>
    public const string RabbitMqRoutingKey = "messaging.rabbitmq.destination.routing_key";

    /// <summary>messaging.rabbitmq.message.delivery_tag</summary>
    public const string RabbitMqDeliveryTag = "messaging.rabbitmq.message.delivery_tag";

    /// <summary>messaging.message.id</summary>
    public const string MessageId = "messaging.message.id";

    /// <summary>messaging.message.conversation_id (the correlation id)</summary>
    public const string ConversationId = "messaging.message.conversation_id";

    /// <summary>messaging.message.body.size</summary>
    public const string BodySize = "messaging.message.body.size";

    /// <summary>server.address</summary>
    public const string ServerAddress = "server.address";

    /// <summary>server.port</summary>
    public const string ServerPort = "server.port";

    /// <summary>error.type</summary>
    public const string ErrorType = "error.type";

    /// <summary>easynetq.message.type - the resolved message type's display name</summary>
    public const string MessageType = "easynetq.message.type";

    /// <summary>easynetq.ack.decision - ack, nack_requeue, nack_discard, reject or handled</summary>
    public const string AckDecision = "easynetq.ack.decision";

    /// <summary>easynetq.error.queue - true on the error-queue republish span</summary>
    public const string ErrorQueue = "easynetq.error.queue";
}
