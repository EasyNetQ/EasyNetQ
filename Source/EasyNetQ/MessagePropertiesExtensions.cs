using System.Collections.Immutable;
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
        => properties.SetHeader(ConfirmationIdHeader, Encoding.UTF8.GetBytes(confirmationId.ToString()));

    public static MessageProperties SetHeader(in this MessageProperties source, string key, object? value)
        => source with { Headers = EnsureHeadersImmutable(source.Headers).SetItem(key, value) };

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

        if (source is { HeadersPresent: true, Headers: { } })
            basicProperties.Headers = source.Headers as IDictionary<string, object?> ?? source.Headers.ToImmutableDictionary();
    }

    private static ImmutableDictionary<string, object?> EnsureHeadersImmutable(IReadOnlyDictionary<string, object?>? headers)
    {
        return headers switch
        {
            null => ImmutableDictionary<string, object?>.Empty,
            ImmutableDictionary<string, object?> immutable => immutable,
            _ => ImmutableDictionary.CreateRange(headers)
        };
    }
}
