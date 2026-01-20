using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace EasyNetQ;

/// <summary>
///     Register serializer based on System.Text.Json
/// </summary>
public static class EasyNetQBuilderSystemTextJsonExtensions
{
    /// <summary>
    ///     Enables serializer based on System.Text.Json
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJson(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<ISerializer, SystemTextJsonSerializer>();
        return builder;
    }

    /// <summary>
    ///     Enables serializer based on System.Text.Json with custom options
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJson(this IEasyNetQBuilder builder, JsonSerializerOptions options)
    {
        builder.Services.AddSingleton<ISerializer>(_ => new SystemTextJsonSerializer(options));
        return builder;
    }

    /// <summary>
    ///     Enables serializer based on System.Text.Json v2
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJsonV2(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<ISerializer, SystemTextJsonSerializerV2>();
        return builder;
    }

    /// <summary>
    ///     Enables serializer based on System.Text.Json v2 with custom options
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJsonV2(this IEasyNetQBuilder builder, JsonSerializerOptions options)
    {
        builder.Services.AddSingleton<ISerializer>(_ => new SystemTextJsonSerializerV2(options));
        return builder;
    }
}
