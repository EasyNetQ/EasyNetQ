using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace EasyNetQ.Serialization.SystemTextJson;

public class MessagePropertiesConverter : JsonConverter<MessageProperties>
{
    public override MessageProperties? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var parsed = JsonElement.ParseValue(ref reader);
        if (parsed.ValueKind == JsonValueKind.Null) return null;
        if (parsed.ValueKind == JsonValueKind.Object)
        {
            var messageProperties = new MessageProperties();
            if (parsed.TryGetProperty(options.ConvertName("ContentType"), out var contentType))
                messageProperties.ContentType = contentType.GetString();
            if (parsed.TryGetProperty(options.ConvertName("ContentEncoding"), out var contentEncoding))
                messageProperties.ContentEncoding = contentEncoding.GetString();
            if (parsed.TryGetProperty(options.ConvertName("DeliveryMode"), out var deliveryMode))
                messageProperties.DeliveryMode = deliveryMode.GetByte();
            if (parsed.TryGetProperty(options.ConvertName("Priority"), out var priority))
                messageProperties.Priority = priority.GetByte();
            if (parsed.TryGetProperty(options.ConvertName("CorrelationId"), out var correlationId))
                messageProperties.CorrelationId = correlationId.GetString();
            if (parsed.TryGetProperty(options.ConvertName("ReplyTo"), out var replyTo))
                messageProperties.ReplyTo = replyTo.GetString();
            if (parsed.TryGetProperty(options.ConvertName("Expiration"), out var expiration))
                messageProperties.Expiration = expiration.Deserialize<TimeSpan>();
            if (parsed.TryGetProperty(options.ConvertName("MessageId"), out var messageId))
                messageProperties.MessageId = messageId.GetString();
            if (parsed.TryGetProperty(options.ConvertName("Timestamp"), out var timestamp))
                messageProperties.Timestamp = timestamp.GetInt64();
            if (parsed.TryGetProperty(options.ConvertName("Type"), out var type))
                messageProperties.Type = type.GetString();
            if (parsed.TryGetProperty(options.ConvertName("UserId"), out var userId))
                messageProperties.UserId = userId.GetString();
            if (parsed.TryGetProperty(options.ConvertName("AppId"), out var appId))
                messageProperties.AppId = appId.GetString();
            if (parsed.TryGetProperty(options.ConvertName("ClusterId"), out var clusterId))
                messageProperties.ClusterId = clusterId.GetString();
            if (parsed.TryGetProperty(options.ConvertName("Headers"), out var headers))
                messageProperties.Headers = headers.ConvertJsonToHeaders(options);
            return messageProperties;
        }
        throw new ArgumentOutOfRangeException(nameof(reader.TokenType), reader.TokenType, null);
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
            foreach (var kvp in value.Headers)
                headersJson.AddHeaderToJson(kvp.Key, kvp.Value, options);
            json.Add(options.ConvertName("Headers"), headersJson);
        }
        json.WriteTo(writer);
    }
}
