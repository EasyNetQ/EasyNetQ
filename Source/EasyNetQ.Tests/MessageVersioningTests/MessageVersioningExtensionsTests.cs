using EasyNetQ.MessageVersioning;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.MessageVersioningTests;

public class MessageVersioningExtensionsTests
{
    [Fact]
    public void When_using_EnableMessageVersioning_extension_method_required_services_are_registered()
    {
        var serviceCollection = new ServiceCollection();
        new EasyNetQBuilder(serviceCollection).EnableMessageVersioning();

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
