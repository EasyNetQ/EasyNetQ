using RabbitMQ.Client;

namespace EasyNetQ;

/// <summary>
///     Represents various properties of a message
/// </summary>
public readonly record struct MessageProperties
{
    public static MessageProperties Empty => default;

    internal MessageProperties(IBasicProperties basicProperties)
    {
        ContentType = basicProperties.ContentType;
        ContentEncoding = basicProperties.ContentEncoding;
        DeliveryMode = basicProperties.DeliveryMode;
        Priority = basicProperties.Priority;
        CorrelationId = basicProperties.CorrelationId;
        ReplyTo = basicProperties.ReplyTo;
        Expiration = int.TryParse(basicProperties.Expiration, out var expirationMilliseconds)
            ? TimeSpan.FromMilliseconds(expirationMilliseconds)
            : null;
        MessageId = basicProperties.MessageId;
        Timestamp = basicProperties.Timestamp.UnixTime;
        Type = basicProperties.Type;
        UserId = basicProperties.UserId;
        AppId = basicProperties.AppId;
        ClusterId = basicProperties.ClusterId;
        Headers = basicProperties.Headers;
    }

    /// <summary>
    ///     MIME Content type
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    ///     MIME content encoding
    /// </summary>
    public string? ContentEncoding { get; init; }

    /// <summary>
    ///     Various headers
    /// </summary>
    public IDictionary<string, object?>? Headers { get; init; }

    /// <summary>
    ///     non-persistent (1) or persistent (2)
    /// </summary>
    public byte DeliveryMode { get; init; }

    /// <summary>
    ///     Message priority, 0 to 9
    /// </summary>
    public byte Priority { get; init; }

    /// <summary>
    ///     Application correlation identifier
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Destination to reply to
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    ///     Message expiration specification
    /// </summary>
    public TimeSpan? Expiration { get; init; }

    /// <summary>
    ///     Application message identifier
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    ///     Message timestamp
    /// </summary>
    public long Timestamp { get; init; }

    /// <summary>
    ///     Message type name
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    ///     Creating user id
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    ///     Application id
    /// </summary>
    public string? AppId { get; init; }

    /// <summary>
    ///     Intra-cluster routing identifier
    /// </summary>
    public string? ClusterId { get; init; }

    /// <summary>
    ///     True if <see cref="ContentType"/> is present
    /// </summary>
    public bool ContentTypePresent => ContentType != default;

    /// <summary>
    ///     True if <see cref="ContentEncoding"/> is present
    /// </summary>
    public bool ContentEncodingPresent => ContentEncoding != default;

    /// <summary>
    ///     True if <see cref="Headers"/> is present
    /// </summary>
    public bool HeadersPresent => Headers?.Count > 0;

    /// <summary>
    ///     True if <see cref="DeliveryMode"/> is present
    /// </summary>
    public bool DeliveryModePresent => DeliveryMode != default;

    /// <summary>
    ///     True if <see cref="Priority"/> is present
    /// </summary>
    public bool PriorityPresent => Priority != default;

    /// <summary>
    ///     True if <see cref="CorrelationId"/> is present
    /// </summary>
    public bool CorrelationIdPresent => CorrelationId != default;

    /// <summary>
    ///     True if <see cref="ReplyTo"/> is present
    /// </summary>
    public bool ReplyToPresent => ReplyTo != default;

    /// <summary>
    ///     True if <see cref="Expiration"/> is present
    /// </summary>
    public bool ExpirationPresent => Expiration != null;

    /// <summary>
    ///     True if <see cref="MessageId"/> is present
    /// </summary>
    public bool MessageIdPresent => MessageId != default;

    /// <summary>
    ///     True if <see cref="Timestamp"/> is present
    /// </summary>
    public bool TimestampPresent => Timestamp != default;

    /// <summary>
    ///     True if <see cref="Type"/> is present
    /// </summary>
    public bool TypePresent => Type != default;

    /// <summary>
    ///     True if <see cref="UserId"/> is present
    /// </summary>
    public bool UserIdPresent => UserId != default;

    /// <summary>
    ///     True if <see cref="AppId"/> is present
    /// </summary>
    public bool AppIdPresent => AppId != default;

    /// <summary>
    ///     True if <see cref="ClusterId"/> is present
    /// </summary>
    public bool ClusterIdPresent => ClusterId != default;
}
