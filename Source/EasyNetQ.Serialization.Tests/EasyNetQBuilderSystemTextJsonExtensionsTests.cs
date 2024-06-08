using EasyNetQ.Serialization.NewtonsoftJson;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Serialization.Tests;

public class EasyNetQBuilderSystemTextJsonExtensionsTests
{
    [Theory]
    [MemberData(nameof(GetSerializerRegisterActions))]
    public void Should_register_serializer(Action<IEasyNetQBuilder> register, Type serializerType)
    {
        var serviceCollection = new ServiceCollection();
        var easyNetQBuilder = new EasyNetQBuilder(serviceCollection);

        register(easyNetQBuilder);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var registeredServiceDescriptor = serviceProvider.GetService<ISerializer>();

        Assert.NotNull(registeredServiceDescriptor);
        Assert.Equal(serializerType, registeredServiceDescriptor.GetType());
    }

    public static IEnumerable<object[]> GetSerializerRegisterActions()
    {
        yield return [GetRegisterAction(x => x.EnableNewtonsoftJson()), typeof(NewtonsoftJsonSerializer)];
        yield return [GetRegisterAction(x => x.EnableSystemTextJson()), typeof(SystemTextJsonSerializer)];
    }

    private static Action<IEasyNetQBuilder> GetRegisterAction(Action<IEasyNetQBuilder> action) => action;
}
