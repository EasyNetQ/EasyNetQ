using EasyNetQ.Serialization.NewtonsoftJson;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Serialization.Tests;

public class EasyNetQBuilderSystemTextJsonExtensionsTests
{
    [Fact]
    public void UseNewtonsoftJson_should_register_legacy_serializer()
    {
        var serviceCollection = new ServiceCollection();
        var easyNetQBuilder = new EasyNetQBuilder(serviceCollection);

        easyNetQBuilder.UseNewtonsoftJson();

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        serviceProvider.GetService<ISerializer>().Should().BeOfType<NewtonsoftJsonSerializer>();
    }

    [Fact]
    public void UseSystemTextJson_should_register_message_serializer()
    {
        var serviceCollection = new ServiceCollection();
        var easyNetQBuilder = new EasyNetQBuilder(serviceCollection);

        easyNetQBuilder.UseSystemTextJson();

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        serviceProvider.GetService<IMessageSerializer>().Should().BeOfType<SystemTextJsonMessageSerializer>();
    }

    [Fact]
    public void A_registered_legacy_serializer_should_be_wrapped_by_the_default_message_serializer_registration()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ("host=localhost").UseNewtonsoftJson();

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        serviceProvider.GetService<IMessageSerializer>().Should().BeOfType<EasyNetQ.Serialization.LegacyMessageSerializerAdapter>();
    }

    [Fact]
    public void The_default_message_serializer_should_be_system_text_json()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ("host=localhost");

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        serviceProvider.GetService<IMessageSerializer>().Should().BeOfType<SystemTextJsonMessageSerializer>();
    }
}
