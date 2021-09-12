using EasyNetQ.DI;
using EasyNetQ.Serialization.NewtonsoftJson;

// ReSharper disable once CheckNamespace
namespace EasyNetQ
{
    /// <summary>
    ///     Register serializer based on Newtonsoft.Json
    /// </summary>
    public static class ServiceRegisterExtensions
    {
        /// <summary>
        ///     Enables serializer based on Newtonsoft.Json
        /// </summary>
        /// <param name="serviceRegister">The register</param>
        public static IServiceRegister EnableNewtonsoftJson(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<ISerializer, NewtonsoftJsonSerializer>();
        }

        /// <summary>
        ///     Enables serializer based on Newtonsoft.Json
        /// </summary>
        /// <param name="serviceRegister">The register</param>
        /// <param name="settings">The custom settings</param>
        public static IServiceRegister EnableNewtonsoftJson(
            this IServiceRegister serviceRegister, Newtonsoft.Json.JsonSerializerSettings settings
        )
        {
            return serviceRegister.Register<ISerializer>(_ => new NewtonsoftJsonSerializer(settings));
        }
    }
}
