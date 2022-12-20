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
    public static IServiceRegister EnableSystemTextJson(this IServiceRegister serviceRegister)
        => serviceRegister.Register<ISerializer, SystemTextJsonSerializer>();

    /// <summary>
    ///     Enables serializer based on System.Text.Json
    /// </summary>
    public static IServiceRegister EnableSystemTextJson(this IServiceRegister serviceRegister, System.Text.Json.JsonSerializerOptions options)
        => serviceRegister.Register<ISerializer>(_ => new SystemTextJsonSerializer(options));

    /// <summary>
    ///     Enables serializer based on System.Text.Json
    /// </summary>
    public static IServiceRegister EnableSystemTextJsonV2(this IServiceRegister serviceRegister)
        => serviceRegister.Register<ISerializer, SystemTextJsonSerializerV2>();

    /// <summary>
    ///     Enables serializer based on System.Text.Json
    /// </summary>
    public static IServiceRegister EnableSystemTextJsonV2(this IServiceRegister serviceRegister, System.Text.Json.JsonSerializerOptions options)
        => serviceRegister.Register<ISerializer>(_ => new SystemTextJsonSerializerV2(options));
}
