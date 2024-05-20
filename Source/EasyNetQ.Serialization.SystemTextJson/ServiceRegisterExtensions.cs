using EasyNetQ.DI;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace EasyNetQ.Extensions
{
    /// <summary>
    ///     Register serializer based on System.Text.Json
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Enables serializer based on System.Text.Json
        /// </summary>
        public static IServiceCollection EnableSystemTextJson(this IServiceCollection services)
        {
            services.AddSingleton<ISerializer, SystemTextJsonSerializer>();
            return services;
        }

        /// <summary>
        ///     Enables serializer based on System.Text.Json with custom options
        /// </summary>
        public static IServiceCollection EnableSystemTextJson(this IServiceCollection services, JsonSerializerOptions options)
        {
            services.AddSingleton<ISerializer>(_ => new SystemTextJsonSerializer(options));
            return services;
        }

        /// <summary>
        ///     Enables serializer based on System.Text.Json v2
        /// </summary>
        public static IServiceCollection EnableSystemTextJsonV2(this IServiceCollection services)
        {
            services.AddSingleton<ISerializer, SystemTextJsonSerializerV2>();
            return services;
        }

        /// <summary>
        ///     Enables serializer based on System.Text.Json v2 with custom options
        /// </summary>
        public static IServiceCollection EnableSystemTextJsonV2(this IServiceCollection services, JsonSerializerOptions options)
        {
            services.AddSingleton<ISerializer>(_ => new SystemTextJsonSerializerV2(options));
            return services;
        }
    }
}
