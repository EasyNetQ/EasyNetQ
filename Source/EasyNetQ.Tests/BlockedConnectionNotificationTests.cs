using EasyNetQ.Tests.Mocking;

namespace EasyNetQ.Tests;

public class When_a_connection_becomes_blocked
{
    private readonly MockBuilder mockBuilder = new();

    [Fact]
    public async Task Should_raise_blocked_event()
    {
        await using var _ = await mockBuilder.ProducerConnection.CreateChannelAsync();

        var blocked = false;
        mockBuilder.Bus.Advanced.Blocked += (_, _) => blocked = true;
        //mockBuilder.Connection.ConnectionBlockedAsync += Raise.EventWith(new ConnectionBlockedEventArgs("some reason"));

        Assert.True(blocked);
    }
}

public class When_a_connection_becomes_unblocked
{
    private readonly MockBuilder mockBuilder = new();

    [Fact]
    public async Task Should_raise_unblocked_event()
    {
        await using var _ = await mockBuilder.ProducerConnection.CreateChannelAsync();

        var blocked = true;
        mockBuilder.Bus.Advanced.Unblocked += (_, _) => blocked = false;
        //mockBuilder.Connection.ConnectionUnblockedAsync += Raise.EventWith(EventArgs.Empty);
        Assert.False(blocked);
    }
}
