using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using RabbitMQ.Client;

namespace EasyNetQ.Serialization.SystemTextJson;

internal static class JsonHeaderExtensions
{
    public static Dictionary<string, object> ConvertJsonToHeaders(this JsonElement jsonElement, JsonSerializerOptions options)
    {
        var headers = new Dictionary<string, object>();
        using (var enumerator = jsonElement.EnumerateObject())
        {
            foreach (var property in enumerator)
            {
                var type = property.Value.GetProperty(options.ConvertName("Type")).GetInt32();
                var value = property.Value.GetProperty(options.ConvertName("Value"));
                headers.Add(property.Name, (type, value).ConvertJsonToObject(options));
            }
        }
        return headers;
    }

    public static void AddHeaderToJson(this JsonObject json, string name, object value, JsonSerializerOptions options)
    {
        var (valueType, valueJson) = value.ConvertToTypeAndJson(options);
        var valueContainer = new JsonObject
        {
            { options.ConvertName("Type"), JsonValue.Create(valueType) },
            { options.ConvertName("Value"), valueJson }
        };
        json.Add(name, valueContainer);
    }

    private static (int, JsonNode) ConvertToTypeAndJson(this object value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                return (JsonHeaderType.Null, null);
            case bool boolValue:
                return (JsonHeaderType.Bool, JsonValue.Create(boolValue));
            case byte byteValue:
                return (JsonHeaderType.Byte, JsonValue.Create(byteValue));
            case sbyte sByteValue:
                return (JsonHeaderType.SByte, JsonValue.Create(sByteValue));
            case short int16Value:
                return (JsonHeaderType.Int16, JsonValue.Create(int16Value));
            case int int32Value:
                return (JsonHeaderType.Int32, JsonValue.Create(int32Value));
            case uint uint32Value:
                return (JsonHeaderType.UInt32, JsonValue.Create(uint32Value));
            case long int64Value:
                return (JsonHeaderType.Int64, JsonValue.Create(int64Value));
            case float singleValue:
                return (JsonHeaderType.Single, JsonValue.Create(singleValue));
            case double doubleValue:
                return (JsonHeaderType.Double, JsonValue.Create(doubleValue));
            case decimal decimalValue:
                return (JsonHeaderType.Decimal, JsonValue.Create(decimalValue));
            case AmqpTimestamp amqpTimestamp:
                return (JsonHeaderType.AmqpTimestamp, JsonValue.Create(amqpTimestamp.UnixTime));
            case string stringValue:
                return (JsonHeaderType.String, JsonValue.Create(stringValue));
            case byte[] bytesValue:
                return (JsonHeaderType.Bytes, JsonValue.Create(bytesValue));
            case IList listValue:
                var list = new List<JsonNode>();
                foreach (var listItem in listValue)
                {
                    var (listItemType, listItemJson) = ConvertToTypeAndJson(listItem, options);
                    list.Add(new JsonObject
                    {
                        { options.ConvertName("Type"), JsonValue.Create(listItemType) },
                        { options.ConvertName("Value"), listItemJson }
                    });
                }
                return (JsonHeaderType.List, new JsonArray(list.ToArray()));
            case IDictionary dictionaryValue:
                var dictionaryJson = new JsonObject();
                foreach (DictionaryEntry dictionaryItem in dictionaryValue)
                {
                    var (dictionaryItemType, dictionaryItemJson) = ConvertToTypeAndJson(dictionaryItem.Value, options);
                    dictionaryJson.Add(
                        options.ConvertName(dictionaryItem.Key.ToString()!),
                        new JsonObject
                        {
                            { options.ConvertName("Type"), JsonValue.Create(dictionaryItemType) },
                            { options.ConvertName("Value"), dictionaryItemJson }
                        }
                    );
                }
                return (JsonHeaderType.Dictionary, dictionaryJson);
            case BinaryTableValue binaryTableValue:
                return (JsonHeaderType.BinaryTable, JsonValue.Create(binaryTableValue.Bytes));
        }

        throw new InternalBufferOverflowException();
    }

    private static object ConvertJsonToObject(this (int Type, JsonElement Value) valueContainer, JsonSerializerOptions options)
    {
        var (type, json) = valueContainer;
        switch (type)
        {
            case JsonHeaderType.Null:
                return null;
            case JsonHeaderType.Int32:
                return json.GetInt32();
            case JsonHeaderType.UInt32:
                return json.GetUInt32();
            case JsonHeaderType.Int64:
                return json.GetInt64();
            case JsonHeaderType.Bool:
                return json.GetBoolean();
            case JsonHeaderType.Single:
                return json.GetSingle();
            case JsonHeaderType.Double:
                return json.GetDouble();
            case JsonHeaderType.Decimal:
                return json.GetDecimal();
            case JsonHeaderType.Byte:
                return json.GetByte();
            case JsonHeaderType.SByte:
                return json.GetSByte();
            case JsonHeaderType.Int16:
                return json.GetInt16();
            case JsonHeaderType.String:
                return json.GetString();
            case JsonHeaderType.Bytes:
                return json.GetBytesFromBase64();
            case JsonHeaderType.AmqpTimestamp:
                return new AmqpTimestamp(json.GetInt64());
            case JsonHeaderType.List:
                var list = new List<object>();
                using (var enumerator = json.EnumerateArray())
                {
                    foreach (var arrayItem in enumerator)
                    {
                        var arrayItemType = arrayItem.GetProperty(options.ConvertName("Type")).GetInt32();
                        var arrayItemValue = arrayItem.GetProperty(options.ConvertName("Value"));
                        list.Add((arrayItemType, arrayItemValue).ConvertJsonToObject(options));
                    }
                }
                return list;
            case JsonHeaderType.Dictionary:
                var dictionary = new Dictionary<string, object>();
                using (var enumerator = json.EnumerateObject())
                {
                    foreach (var objectItem in enumerator)
                    {
                        var objectItemType = objectItem.Value.GetProperty(options.ConvertName("Type")).GetInt32();
                        var objectItemValue = objectItem.Value.GetProperty(options.ConvertName("Value"));
                        dictionary.Add(objectItem.Name, (objectItemType, objectItemValue).ConvertJsonToObject(options));
                    }
                }
                return dictionary;
            case JsonHeaderType.BinaryTable:
                return new BinaryTableValue(json.GetBytesFromBase64());
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}
