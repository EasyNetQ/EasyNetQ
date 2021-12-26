// ReSharper disable InconsistentNaming
using EasyNetQ.Tests.Mocking;
using NSubstitute;
using RabbitMQ.Client.Events;
using System;
using Xunit;

namespace EasyNetQ.Tests;

public class When_a_connection_becomes_blocked
{
    private readonly MockBuilder mockBuilder;

    public When_a_connection_becomes_blocked()
    {
        mockBuilder = new MockBuilder();
    }

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
    private readonly MockBuilder mockBuilder;

    public When_a_connection_becomes_unblocked()
    {
        mockBuilder = new MockBuilder();
    }

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
