using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace EasyNetQ;

/// <summary>
///     Register serializer based on System.Text.Json
/// </summary>
public static class EasyNetQBuilderSystemTextJsonExtensions
{
    /// <summary>
    ///     Enables the System.Text.Json message serializer (this is also the default)
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJson(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IMessageSerializer>(new SystemTextJsonMessageSerializer());
        return builder;
    }

    /// <summary>
    ///     Enables the System.Text.Json message serializer with custom options
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJson(this IEasyNetQBuilder builder, JsonSerializerOptions options)
    {
        builder.Services.AddSingleton<IMessageSerializer>(new SystemTextJsonMessageSerializer(options));
        return builder;
    }

    /// <summary>
    ///     Enables the System.Text.Json message serializer with a source-generated contract context
    ///     (reflection-free and Native AOT safe)
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJson(this IEasyNetQBuilder builder, JsonSerializerContext context)
    {
        builder.Services.AddSingleton<IMessageSerializer>(new SystemTextJsonMessageSerializer(context));
        return builder;
    }

    /// <summary>
    ///     Enables the System.Text.Json message serializer. Kept for 8.x compatibility; identical to
    ///     <see cref="UseSystemTextJson(IEasyNetQBuilder)" />.
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJsonV2(this IEasyNetQBuilder builder) => builder.UseSystemTextJson();

    /// <summary>
    ///     Enables the System.Text.Json message serializer with custom options. Kept for 8.x compatibility; identical
    ///     to <see cref="UseSystemTextJson(IEasyNetQBuilder, JsonSerializerOptions)" />.
    /// </summary>
    public static IEasyNetQBuilder UseSystemTextJsonV2(this IEasyNetQBuilder builder, JsonSerializerOptions options)
        => builder.UseSystemTextJson(options);
}
