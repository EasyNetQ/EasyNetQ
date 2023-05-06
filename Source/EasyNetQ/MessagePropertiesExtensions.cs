using RabbitMQ.Client;
using System.Globalization;
using EasyNetQ.Internals;

namespace EasyNetQ;

/// <summary>
///     Represents various properties of a message
/// </summary>
public static class MessagePropertiesExtensions
{
    internal const string ConfirmationIdHeader = "EasyNetQ.Confirmation.Id";

    public static MessageProperties SetHeader(in this MessageProperties source, string key, object? value)
    {
        var headers = source.Headers ?? new Dictionary<string, object?>();
        headers[key] = value;
        return source with { Headers = headers };
    }

    internal static MessageProperties SetConfirmationId(in this MessageProperties properties, ulong confirmationId)
        => properties.SetHeader(ConfirmationIdHeader, NumberHelpers.FormatULongToBytes(confirmationId));

    internal static bool TryGetConfirmationId(in this MessageProperties properties, out ulong confirmationId)
    {
        confirmationId = 0;
        return properties.Headers != null &&
               properties.Headers.TryGetValue(ConfirmationIdHeader, out var value) &&
               value is byte[] bytesValue &&
               NumberHelpers.TryParseULongFromBytes(bytesValue, out confirmationId);
    }

    public static void CopyTo(in this MessageProperties source, IBasicProperties basicProperties)
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

        if (source is { HeadersPresent: true, Headers: not null })
            basicProperties.Headers = new Dictionary<string, object?>(source.Headers);
    }
}
