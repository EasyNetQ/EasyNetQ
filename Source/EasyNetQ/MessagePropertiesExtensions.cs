using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EasyNetQ;

/// <summary>
///     Represents various properties of a message
/// </summary>
public static class MessagePropertiesExtensions
{
    internal const string ConfirmationIdHeader = "EasyNetQ.Confirmation.Id";

    internal static MessageProperties SetConfirmationId(this MessageProperties properties, ulong confirmationId)
    {
        properties.Headers[ConfirmationIdHeader] = confirmationId.ToString();
        return properties;
    }

    internal static bool TryGetConfirmationId(this MessageProperties properties, out ulong confirmationId)
    {
        confirmationId = 0;
        return properties.Headers.TryGetValue(ConfirmationIdHeader, out var value) &&
               ulong.TryParse(Encoding.UTF8.GetString(value as byte[] ?? Array.Empty<byte>()), out confirmationId);
    }

    public static void CopyFrom(this MessageProperties source, IBasicProperties basicProperties)
    {
        if (basicProperties.IsContentTypePresent()) source.ContentType = basicProperties.ContentType;
        if (basicProperties.IsContentEncodingPresent()) source.ContentEncoding = basicProperties.ContentEncoding;
        if (basicProperties.IsDeliveryModePresent()) source.DeliveryMode = basicProperties.DeliveryMode;
        if (basicProperties.IsPriorityPresent()) source.Priority = basicProperties.Priority;
        if (basicProperties.IsCorrelationIdPresent()) source.CorrelationId = basicProperties.CorrelationId;
        if (basicProperties.IsReplyToPresent()) source.ReplyTo = basicProperties.ReplyTo;
        if (basicProperties.IsExpirationPresent())
            source.Expiration = int.TryParse(basicProperties.Expiration, out var expirationMilliseconds)
                ? TimeSpan.FromMilliseconds(expirationMilliseconds)
                : default;
        if (basicProperties.IsMessageIdPresent()) source.MessageId = basicProperties.MessageId;
        if (basicProperties.IsTimestampPresent()) source.Timestamp = basicProperties.Timestamp.UnixTime;
        if (basicProperties.IsTypePresent()) source.Type = basicProperties.Type;
        if (basicProperties.IsUserIdPresent()) source.UserId = basicProperties.UserId;
        if (basicProperties.IsAppIdPresent()) source.AppId = basicProperties.AppId;
        if (basicProperties.IsClusterIdPresent()) source.ClusterId = basicProperties.ClusterId;

        if (basicProperties.IsHeadersPresent() && basicProperties.Headers?.Count > 0)
            source.Headers = new Dictionary<string, object?>(basicProperties.Headers);
    }

    public static void CopyTo(this MessageProperties source, IBasicProperties basicProperties)
    {
        if (source.ContentTypePresent) basicProperties.ContentType = source.ContentType;
        if (source.ContentEncodingPresent) basicProperties.ContentEncoding = source.ContentEncoding;
        if (source.DeliveryModePresent) basicProperties.DeliveryMode = source.DeliveryMode;
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

        if (source.HeadersPresent)
            basicProperties.Headers = new Dictionary<string, object?>(source.Headers);
    }
}
