using Castle.Windsor;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.DI.Tests;

public class BusCreationTest
{
    [Fact]
    public void ShouldCreateBusForCastleWindsor()
    {
        // arrange
        var container = new WindsorContainer();

        // act
        container.RegisterEasyNetQ(
            connectionConfigurationFactory: serviceResolver =>
            {
                var connection = new ConnectionConfiguration();
                connection.Hosts.Add(new HostConfiguration("localhost", 1));

                return connection;
            },
            services => { }
        );

        var bus = container.Resolve<IBus>();

        // assert
        bus.Should().NotBeNull();
    }
}