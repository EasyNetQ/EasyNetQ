using System;

namespace EasyNetQ.Serialization.SystemTextJson;

public class SystemObjectNewtonsoftCompatibleConverter : System.Text.Json.Serialization.JsonConverter<object>
{
    public override object? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case System.Text.Json.JsonTokenType.True:
                return true;
            case System.Text.Json.JsonTokenType.False:
                return false;
            case System.Text.Json.JsonTokenType.Number:
                return reader.TryGetInt64(out var longValue) ? longValue : reader.GetDouble();
            case System.Text.Json.JsonTokenType.String:
                return reader.TryGetDateTime(out var datetimeValue) ? datetimeValue : reader.GetString();
            default:
                {
                    using var document = System.Text.Json.JsonDocument.ParseValue(ref reader);
                    return document.RootElement.Clone();
                }
        }
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, object value, System.Text.Json.JsonSerializerOptions options)
    {
        throw new InvalidOperationException("Should not get here.");
    }
}
