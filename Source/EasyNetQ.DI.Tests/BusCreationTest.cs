namespace EasyNetQ.DI.Tests;

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters

public class BusCreationTest
{
    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void ShouldCreateBus(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(
            sr => RabbitHutch.RegisterBus(
                sr,
                _ =>
                {
                    var connection = new ConnectionConfiguration();
                    connection.Hosts.Add(new HostConfiguration("localhost", 1));
                    return connection;
                },
                _ => { }
            )
        );

        resolver.Resolve<IBus>().Should().NotBeNull();
    }
}
