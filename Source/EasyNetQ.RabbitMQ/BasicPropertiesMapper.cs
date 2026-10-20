using System.Globalization;
using EasyNetQ.Internals;
using RabbitMQ.Client;

namespace EasyNetQ;

/// <summary>
///     Maps between <see cref="MessageProperties" /> and RabbitMQ.Client's basic properties. This is the only place
///     where message properties touch AMQP types; MessageProperties itself is transport-neutral.
/// </summary>
public static class BasicPropertiesMapper
{

    /// <summary>
    ///     Creates <see cref="MessageProperties" /> from a received message's basic properties
    /// </summary>
    public static MessageProperties FromBasicProperties(IReadOnlyBasicProperties basicProperties) => new()
    {
        ContentType = basicProperties.ContentType,
        ContentEncoding = basicProperties.ContentEncoding,
        DeliveryMode = (byte)basicProperties.DeliveryMode,
        Priority = basicProperties.Priority,
        CorrelationId = basicProperties.CorrelationId,
        ReplyTo = basicProperties.ReplyTo,
        Expiration = int.TryParse(basicProperties.Expiration, out var expirationMilliseconds)
            ? TimeSpan.FromMilliseconds(expirationMilliseconds)
            : null,
        MessageId = basicProperties.MessageId,
        Timestamp = basicProperties.Timestamp.UnixTime,
        Type = basicProperties.Type,
        UserId = basicProperties.UserId,
        AppId = basicProperties.AppId,
        ClusterId = basicProperties.ClusterId,
        Headers = basicProperties.Headers
    };

    /// <summary>
    ///     Copies <paramref name="source" /> onto a fresh outgoing <see cref="IBasicProperties" />
    /// </summary>
    public static void CopyTo(in this MessageProperties source, IBasicProperties basicProperties)
    {
        if (source.ContentTypePresent) basicProperties.ContentType = source.ContentType;
        if (source.ContentEncodingPresent) basicProperties.ContentEncoding = source.ContentEncoding;
        if (source.DeliveryModePresent) basicProperties.DeliveryMode = (DeliveryModes)source.DeliveryMode;
        if (source.PriorityPresent) basicProperties.Priority = source.Priority;
        if (source.CorrelationIdPresent) basicProperties.CorrelationId = source.CorrelationId;
        if (source.ReplyToPresent) basicProperties.ReplyTo = source.ReplyTo;
        if (source.ExpirationPresent)
            basicProperties.Expiration = source.Expiration == null
                ? null
                : ((int)source.Expiration.Value.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);
        if (source.MessageIdPresent) basicProperties.MessageId = source.MessageId;
        if (source.TimestampPresent) basicProperties.Timestamp = new AmqpTimestamp(source.Timestamp);
        if (source.TypePresent) basicProperties.Type = source.Type;
        if (source.UserIdPresent) basicProperties.UserId = source.UserId;
        if (source.AppIdPresent) basicProperties.AppId = source.AppId;
        if (source.ClusterIdPresent) basicProperties.ClusterId = source.ClusterId;

        if (source is { HeadersPresent: true, Headers: not null })
            basicProperties.Headers = source.Headers;
    }

}
