namespace EasyNetQ;

internal static class SpanAttributes
{
    /// <summary>
    ///     Subset of <see href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#messaging-attributes"/>
    /// </summary>
    public static class Messaging
    {
        public const string System = "messaging.system";
        /// <summary> queue or topic </summary>
        public const string Destination = "messaging.destination";
        public const string DestinationKind = "messaging.destination_kind";
        public const string MessageId = "messaging.message_id";
        /// <summary> aka Correlation ID </summary>
        public const string ConversationId = "messaging.conversation_id";
    }

    /// <summary>
    ///     <see href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#rabbitmq"/>
    /// </summary>
    public static class RabbitMq
    {
        public const string RoutingKey = "messaging.rabbitmq.routing_key";
    }

    public static class Payload
    {
        public const string Type = "messaging.payload.type";
    }
}
