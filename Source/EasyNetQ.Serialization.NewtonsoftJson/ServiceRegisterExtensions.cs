using EasyNetQ.Serialization.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;

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
        /// <param name="services">The service collection</param>
        /// <returns>The modified service collection</returns>
        public static IServiceCollection EnableNewtonsoftJson(this IServiceCollection services)
        {
            services.AddSingleton<ISerializer, NewtonsoftJsonSerializer>();
            return services;
        }

        /// <summary>
        ///     Enables serializer based on Newtonsoft.Json with custom settings
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="settings">The custom settings</param>
        /// <returns>The modified service collection</returns>
        public static IServiceCollection EnableNewtonsoftJson(
            this IServiceCollection services, Newtonsoft.Json.JsonSerializerSettings settings
        )
        {
            services.AddSingleton<ISerializer>(_ => new NewtonsoftJsonSerializer(settings));
            return services;
        }
    }
}
