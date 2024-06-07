using EasyNetQ.Serialization.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

/// <summary>
///     Register serializer based on Newtonsoft.Json
/// </summary>
public static class EasyNetQBuilderNewtonsoftExtensions
{
    /// <summary>
    ///     Enables serializer based on Newtonsoft.Json
    /// </summary>
    /// <param name="builder">The service collection</param>
    /// <returns>The modified service collection</returns>
    public static IEasyNetQBuilder EnableNewtonsoftJson(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<ISerializer, NewtonsoftJsonSerializer>();
        return builder;
    }

    /// <summary>
    ///     Enables serializer based on Newtonsoft.Json with custom settings
    /// </summary>
    /// <param name="builder">The service collection</param>
    /// <param name="settings">The custom settings</param>
    /// <returns>The modified service collection</returns>
    public static IEasyNetQBuilder EnableNewtonsoftJson(
        this IEasyNetQBuilder builder, Newtonsoft.Json.JsonSerializerSettings settings
    )
    {
        builder.Services.AddSingleton<ISerializer>(_ => new NewtonsoftJsonSerializer(settings));
        return builder;
    }
}
