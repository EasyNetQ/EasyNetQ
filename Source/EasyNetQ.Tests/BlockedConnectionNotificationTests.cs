using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests;

public class When_a_connection_becomes_blocked
{
    private readonly MockBuilder mockBuilder = new();

    [Fact]
    public void Should_raise_blocked_event()
    {
        using var _ = mockBuilder.ProducerConnection.CreateModel();

        var blocked = false;
        mockBuilder.Bus.Advanced.Blocked += (_, _) => blocked = true;
        mockBuilder.Connection.ConnectionBlocked += Raise.EventWith(new ConnectionBlockedEventArgs("some reason"));

        Assert.True(blocked);
    }
}

public class When_a_connection_becomes_unblocked
{
    private readonly MockBuilder mockBuilder = new();

    [Fact]
    public void Should_raise_unblocked_event()
    {
        using var _ = mockBuilder.ProducerConnection.CreateModel();

        var blocked = true;
        mockBuilder.Bus.Advanced.Unblocked += (_, _) => blocked = false;
        mockBuilder.Connection.ConnectionUnblocked += Raise.EventWith(EventArgs.Empty);
        Assert.False(blocked);
    }
}
