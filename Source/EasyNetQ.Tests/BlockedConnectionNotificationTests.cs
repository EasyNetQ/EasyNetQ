using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests;

public class When_a_connection_becomes_blocked
{
    private readonly MockBuilder mockBuilder = new();

    [Fact]
    public async Task Should_raise_blocked_event()
    {
        AsyncEventHandler<ConnectionBlockedEventArgs> blockedHandlers = null;
        mockBuilder.Connection.ConnectionBlockedAsync += Arg.Do<AsyncEventHandler<ConnectionBlockedEventArgs>>(h => blockedHandlers += h);
        await using var _ = await mockBuilder.ProducerConnection.CreateChannelAsync();
        var blocked = false;
        mockBuilder.Bus.Advanced.Blocked += (_, _) => blocked = true;
        await blockedHandlers?.Invoke(this, new ConnectionBlockedEventArgs("some reason"));
        Assert.True(blocked);
    }
}

public class When_a_connection_becomes_unblocked
{
    private readonly MockBuilder mockBuilder = new();

    [Fact]
    public async Task Should_raise_unblocked_event()
    {
        AsyncEventHandler<AsyncEventArgs> unblockedHandlers = null;
        mockBuilder.Connection.ConnectionUnblockedAsync += Arg.Do<AsyncEventHandler<AsyncEventArgs>>(h => unblockedHandlers += h);

        await using var _ = await mockBuilder.ProducerConnection.CreateChannelAsync();

        var blocked = true;
        mockBuilder.Bus.Advanced.Unblocked += (_, _) => blocked = false;
        await unblockedHandlers?.Invoke(this, new());
        Assert.False(blocked);
    }
}
