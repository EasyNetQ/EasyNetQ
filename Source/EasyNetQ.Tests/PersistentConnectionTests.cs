using EasyNetQ.Persistent;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests;

public class PersistentConnectionTests
{
    [Fact]
    public async Task Should_fail_if_connect_failed()
    {
        var mockBuilder = new MockBuilder();
        mockBuilder.ConnectionFactory.CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>())
            .Returns(Task.FromException<IConnection>(new Exception("Test")));

        using var connection = new PersistentConnection(
            PersistentConnectionType.Producer,
            Substitute.For<ILogger<IPersistentConnection>>(),
            new ConnectionConfiguration(),
            mockBuilder.ConnectionFactory,
            mockBuilder.EventBus
        );

        connection.Status.State.Should().Be(PersistentConnectionState.NotInitialised);

        Assert.Throws<Exception>(() => connection.EnsureConnected());

        connection.Status.State.Should().Be(PersistentConnectionState.Disconnected);
        await mockBuilder.ConnectionFactory.Received().CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>());
    }

    [Fact]
    public async Task Should_connect()
    {
        var mockBuilder = new MockBuilder();
        using var connection = new PersistentConnection(
            PersistentConnectionType.Producer,
            Substitute.For<ILogger<IPersistentConnection>>(),
            new ConnectionConfiguration(),
            mockBuilder.ConnectionFactory,
            mockBuilder.EventBus
        );

        connection.Status.State.Should().Be(PersistentConnectionState.NotInitialised);

        connection.EnsureConnected();

        connection.Status.State.Should().Be(PersistentConnectionState.Connected);
        await mockBuilder.ConnectionFactory.Received(1).CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>());
    }

    [Fact]
    public async Task Should_be_not_connected_if_connection_not_established()
    {
        var mockBuilder = new MockBuilder();
        mockBuilder.ConnectionFactory.CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>())
            .Returns(Task.FromException<IConnection>(new Exception("Test")));

        using var connection = new PersistentConnection(
            PersistentConnectionType.Producer,
            Substitute.For<ILogger<IPersistentConnection>>(),
            new ConnectionConfiguration(),
            mockBuilder.ConnectionFactory,
            mockBuilder.EventBus
        );

        connection.Status.State.Should().Be(PersistentConnectionState.NotInitialised);

        await Assert.ThrowsAsync<Exception>(() => connection.CreateChannelAsync());

        connection.Status.State.Should().Be(PersistentConnectionState.Disconnected);
        await mockBuilder.ConnectionFactory.Received().CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>());
    }

    [Fact]
    public async Task Should_establish_connection_when_persistent_connection_created()
    {
        var mockBuilder = new MockBuilder();
        using var connection = new PersistentConnection(
            PersistentConnectionType.Producer,
            Substitute.For<ILogger<IPersistentConnection>>(),
            new ConnectionConfiguration(),
            mockBuilder.ConnectionFactory,
            mockBuilder.EventBus
        );

        connection.Status.State.Should().Be(PersistentConnectionState.NotInitialised);

        await connection.CreateChannelAsync();

        connection.Status.State.Should().Be(PersistentConnectionState.Connected);
        await mockBuilder.ConnectionFactory.Received(1).CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>());
    }
}
