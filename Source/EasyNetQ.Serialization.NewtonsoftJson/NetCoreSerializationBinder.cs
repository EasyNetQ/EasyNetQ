using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;

namespace EasyNetQ.Serialization.NewtonsoftJson;

internal sealed class NetCoreSerializationBinder : DefaultSerializationBinder
{
    private static readonly Regex Regex = new(
        @"System\.Private\.CoreLib(, Version=[\d\.]+)?(, Culture=[\w-]+)(, PublicKeyToken=[\w\d]+)?");

    private static readonly ConcurrentDictionary<Type, (string? assembly, string? type)> Cache = new();

    public override void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
    {
        base.BindToName(serializedType, out assemblyName, out typeName);

        if (Cache.TryGetValue(serializedType, out var name))
        {
            assemblyName = name.assembly;
            typeName = name.type;
        }
        else
        {
            if (assemblyName?.StartsWith("System.Private.CoreLib") ?? false)
                assemblyName = Regex.Replace(assemblyName, "mscorlib");

            if (typeName?.Contains("System.Private.CoreLib") ?? false)
                typeName = Regex.Replace(typeName, "mscorlib");

            Cache.TryAdd(serializedType, (assemblyName, typeName));
        }
    }
}
