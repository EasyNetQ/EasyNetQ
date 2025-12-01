using System.Text.Json;

namespace EasyNetQ.Serialization.SystemTextJson;

internal static class JsonSerializerOptionsExtensions
{
    public static string ConvertName(this JsonSerializerOptions options, string name) => options.PropertyNamingPolicy?.ConvertName(name) ?? name;
}
