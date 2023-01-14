using RabbitMQ.Client;
using System.Globalization;
using System.Text;

namespace EasyNetQ;

/// <summary>
///     Represents various properties of a message
/// </summary>
public static class MessagePropertiesExtensions
{
    internal const string ConfirmationIdHeader = "EasyNetQ.Confirmation.Id";

    internal static MessageProperties SetConfirmationId(this in MessageProperties properties, ulong confirmationId)
    {
#if NET6_0_OR_GREATER
        // this code path eliminates additional ToString() heap allocation
        Span<char> span = stackalloc char[20]; // length of ulong.MaxValue
        var ok = confirmationId.TryFormat(span, out var charsWritten);
        if (!ok)
            throw new InvalidOperationException("TryFormat failed"); // should not happen
        Span<byte> bytes = stackalloc byte[20];
        var writtenBytes = Encoding.UTF8.GetBytes(span[..charsWritten], bytes);
        return properties.SetHeader(ConfirmationIdHeader, bytes[..writtenBytes].ToArray());
#else
        return properties.SetHeader(ConfirmationIdHeader, Encoding.UTF8.GetBytes(confirmationId.ToString()));
#endif
        }

    public static MessageProperties SetHeader(in this MessageProperties source, string key, object? value)
    {
        var headers = source.Headers ?? new Dictionary<string, object?>();
        headers[key] = value;
        return source with { Headers = headers };
    }

    internal static bool TryGetConfirmationId(this in MessageProperties properties, out ulong confirmationId)
    {
        confirmationId = 0;
        return properties.Headers != null &&
               properties.Headers.TryGetValue(ConfirmationIdHeader, out var value) &&
               ulong.TryParse(Encoding.UTF8.GetString(value as byte[] ?? Array.Empty<byte>()), out confirmationId);
    }

    public static void CopyTo(this in MessageProperties source, IBasicProperties basicProperties)
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
            basicProperties.Headers = source.Headers;
    }
}
