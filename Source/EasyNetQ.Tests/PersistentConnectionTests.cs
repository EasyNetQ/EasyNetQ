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
        await using var mockBuilder = new MockBuilder();
#pragma warning disable IDISP004
        mockBuilder.ConnectionFactory.CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>())
#pragma warning restore IDISP004
            .Returns(Task.FromException<IConnection>(new Exception("Test")));

        using var connection = new PersistentConnection(
            PersistentConnectionType.Producer,
            Substitute.For<ILogger<IPersistentConnection>>(),
            new ConnectionConfiguration(),
            mockBuilder.ConnectionFactory,
            mockBuilder.EventBus
        );

        connection.Status.State.Should().Be(PersistentConnectionState.NotInitialised);

        await Assert.ThrowsAsync<Exception>(async () => await connection.EnsureConnectedAsync());

        connection.Status.State.Should().Be(PersistentConnectionState.Disconnected);
        await mockBuilder.ConnectionFactory.Received().CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>());
    }

    [Fact]
    public async Task Should_connect()
    {
        await using var mockBuilder = new MockBuilder();
        using var connection = new PersistentConnection(
            PersistentConnectionType.Producer,
            Substitute.For<ILogger<IPersistentConnection>>(),
            new ConnectionConfiguration(),
            mockBuilder.ConnectionFactory,
            mockBuilder.EventBus
        );

        connection.Status.State.Should().Be(PersistentConnectionState.NotInitialised);

        await connection.EnsureConnectedAsync();

        connection.Status.State.Should().Be(PersistentConnectionState.Connected);
        await mockBuilder.ConnectionFactory.Received(1).CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>());
    }

    [Fact]
    public async Task Should_be_not_connected_if_connection_not_established()
    {
        await using var mockBuilder = new MockBuilder();
#pragma warning disable IDISP004 // Don't ignore created IDisposable
        mockBuilder.ConnectionFactory.CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>())
#pragma warning restore IDISP004 // Don't ignore created IDisposable
            .Returns(Task.FromException<IConnection>(new Exception("Test")));

        using var connection = new PersistentConnection(
            PersistentConnectionType.Producer,
            Substitute.For<ILogger<IPersistentConnection>>(),
            new ConnectionConfiguration(),
            mockBuilder.ConnectionFactory,
            mockBuilder.EventBus
        );

        connection.Status.State.Should().Be(PersistentConnectionState.NotInitialised);

        await Assert.ThrowsAsync<Exception>(async () =>
           {
               using var channel = await connection.CreateChannelAsync();
           });

        connection.Status.State.Should().Be(PersistentConnectionState.Disconnected);
        await mockBuilder.ConnectionFactory.Received().CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>());
    }

    [Fact]
    public async Task Should_establish_connection_when_persistent_connection_created()
    {
        await using var mockBuilder = new MockBuilder();
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
