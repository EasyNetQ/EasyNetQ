using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace EasyNetQ.Serialization.SystemTextJson;

public class MessagePropertiesConverter : JsonConverter<MessageProperties>
{
    public override MessageProperties Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var parsed = JsonElement.ParseValue(ref reader);
        return parsed.ValueKind switch
        {
            JsonValueKind.Null => default,
            JsonValueKind.Object => new MessageProperties
            {
                ContentType = parsed.TryGetProperty(options.ConvertName("ContentType"), out var contentType) ? contentType.GetString() : null,
                ContentEncoding = parsed.TryGetProperty(options.ConvertName("ContentEncoding"), out var contentEncoding) ? contentEncoding.GetString() : null,
                DeliveryMode = parsed.TryGetProperty(options.ConvertName("DeliveryMode"), out var deliveryMode) ? deliveryMode.GetByte() : default,
                Priority = parsed.TryGetProperty(options.ConvertName("Priority"), out var priority) ? priority.GetByte() : default,
                CorrelationId = parsed.TryGetProperty(options.ConvertName("CorrelationId"), out var correlationId) ? correlationId.GetString() : null,
                ReplyTo = parsed.TryGetProperty(options.ConvertName("ReplyTo"), out var replyTo) ? replyTo.GetString() : null,
                Expiration = parsed.TryGetProperty(options.ConvertName("Expiration"), out var expiration) ? expiration.Deserialize<TimeSpan>() : null,
                MessageId = parsed.TryGetProperty(options.ConvertName("MessageId"), out var messageId) ? messageId.GetString() : null,
                Timestamp = parsed.TryGetProperty(options.ConvertName("Timestamp"), out var timestamp) ? timestamp.GetInt64() : default,
                Type = parsed.TryGetProperty(options.ConvertName("Type"), out var type) ? type.GetString() : null,
                UserId = parsed.TryGetProperty(options.ConvertName("UserId"), out var userId) ? userId.GetString() : null,
                AppId = parsed.TryGetProperty(options.ConvertName("AppId"), out var appId) ? appId.GetString() : null,
                ClusterId = parsed.TryGetProperty(options.ConvertName("ClusterId"), out var clusterId) ? clusterId.GetString() : null,
                Headers = parsed.TryGetProperty(options.ConvertName("Headers"), out var headers)
                    ? headers.ConvertJsonToHeaders(options)
                    : null
            },
            _ => throw new ArgumentOutOfRangeException(nameof(parsed.ValueKind), parsed.ValueKind, null)
        };
    }

    public override void Write(Utf8JsonWriter writer, MessageProperties value, JsonSerializerOptions options)
    {
        var json = new JsonObject();
        if (value.ContentTypePresent)
            json.Add(options.ConvertName("ContentType"), value.ContentType);
        if (value.ContentEncodingPresent)
            json.Add(options.ConvertName("ContentEncoding"), value.ContentEncoding);
        if (value.DeliveryModePresent)
            json.Add(options.ConvertName("DeliveryMode"), value.DeliveryMode);
        if (value.PriorityPresent)
            json.Add(options.ConvertName("Priority"), value.Priority);
        if (value.CorrelationIdPresent)
            json.Add(options.ConvertName("CorrelationId"), value.CorrelationId);
        if (value.ReplyToPresent)
            json.Add(options.ConvertName("ReplyTo"), value.ReplyTo);
        if (value.ExpirationPresent)
            json.Add(options.ConvertName("Expiration"), JsonValue.Create(value.Expiration));
        if (value.MessageIdPresent)
            json.Add(options.ConvertName("MessageId"), value.MessageId);
        if (value.TimestampPresent)
            json.Add(options.ConvertName("Timestamp"), value.Timestamp);
        if (value.TypePresent)
            json.Add(options.ConvertName("Type"), value.Type);
        if (value.UserIdPresent)
            json.Add(options.ConvertName("UserId"), value.UserId);
        if (value.AppIdPresent)
            json.Add(options.ConvertName("AppId"), value.AppId);
        if (value.ClusterIdPresent)
            json.Add(options.ConvertName("ClusterId"), value.ClusterId);
        if (value.HeadersPresent)
        {
            var headersJson = new JsonObject();
            foreach (var kvp in value.Headers!)
                headersJson.AddHeaderToJson(kvp.Key, kvp.Value, options);
            json.Add(options.ConvertName("Headers"), headersJson);
        }
        json.WriteTo(writer);
    }
}
