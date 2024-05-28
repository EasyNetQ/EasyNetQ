using EasyNetQ.Serialization.NewtonsoftJson;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using EasyNetQ.Extensions;

namespace EasyNetQ.Serialization.Tests;

public class ServiceRegisterExtensionsTests
{
    [Theory]
    [MemberData(nameof(GetSerializerRegisterActions))]
    public void Should_register_serializer(Action<IServiceCollection> register, Type serializerType)
    {
        var serviceCollection = new ServiceCollection();
        register(serviceCollection);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var registeredServiceDescriptor = serviceProvider.GetService<ISerializer>();

        Assert.NotNull(registeredServiceDescriptor);
        Assert.Equal(serializerType, registeredServiceDescriptor.GetType());
    }

    public static IEnumerable<object[]> GetSerializerRegisterActions()
    {
        yield return new object[] { GetRegisterAction(x => x.EnableNewtonsoftJson()), typeof(NewtonsoftJsonSerializer) };
        yield return new object[] { GetRegisterAction(x => x.EnableSystemTextJson()), typeof(SystemTextJsonSerializer) };
    }

    private static Action<IServiceCollection> GetRegisterAction(Action<IServiceCollection> action) => action;
}
