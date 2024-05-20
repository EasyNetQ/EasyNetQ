using EasyNetQ.MessageVersioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection TryRegister(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
            services.TryAdd(descriptor);
            return services;
        }
    }

    public class MessageVersioningExtensionsTests
    {
        [Fact]
        public void When_using_EnableMessageVersioning_extension_method_required_services_are_registered()
        {
            var serviceCollection = new ServiceCollection();
            var serviceRegister = serviceCollection as IServiceCollection;

            serviceRegister.TryRegister(typeof(IExchangeDeclareStrategy), typeof(VersionedExchangeDeclareStrategy), ServiceLifetime.Singleton);
            serviceRegister.TryRegister(typeof(IMessageSerializationStrategy), typeof(VersionedMessageSerializationStrategy), ServiceLifetime.Singleton);

            Assert.Contains(serviceCollection, descriptor =>
                descriptor.ServiceType == typeof(IExchangeDeclareStrategy) &&
                descriptor.ImplementationType == typeof(VersionedExchangeDeclareStrategy) &&
                descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(serviceCollection, descriptor =>
                descriptor.ServiceType == typeof(IMessageSerializationStrategy) &&
                descriptor.ImplementationType == typeof(VersionedMessageSerializationStrategy) &&
                descriptor.Lifetime == ServiceLifetime.Singleton);
        }
    }
}
