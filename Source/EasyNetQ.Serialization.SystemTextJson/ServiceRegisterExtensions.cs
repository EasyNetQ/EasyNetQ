using EasyNetQ.DI;
using EasyNetQ.Serialization.SystemTextJson;

// ReSharper disable once CheckNamespace
namespace EasyNetQ;

/// <summary>
///     Register serializer based on System.Text.Json
/// </summary>
public static class ServiceRegisterExtensions
{
    /// <summary>
    ///     Enables serializer based on System.Text.Json
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableSystemTextJson(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register<ISerializer, SystemTextJsonSerializer>();
    }

    public static IServiceRegister EnableSystemTextJson(this IServiceRegister serviceRegister, System.Text.Json.JsonSerializerOptions options)
    {
        return serviceRegister.Register<ISerializer>(_ => new SystemTextJsonSerializer(options));
    }
}
