using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace EasyNetQ.AspNetCore.SignalR.Tests;

/// <summary>Two servers on one broker, clients on each: the scale-out semantics the backplane promises</summary>
public sealed class When_scaling_out_over_two_servers : IAsyncLifetime
{
    private readonly InMemoryBroker broker = new();
    private TestServer a = null!;
    private TestServer b = null!;

    public async ValueTask InitializeAsync()
    {
        a = await TestServer.StartInMemoryAsync(broker, "server-a", TimeSpan.FromSeconds(2));
        b = await TestServer.StartInMemoryAsync(broker, "server-b", TimeSpan.FromSeconds(2));
    }

    public async ValueTask DisposeAsync()
    {
        await a.DisposeAsync();
        await b.DisposeAsync();
    }

    [Fact]
    public async Task Should_send_to_all_clients_on_every_server()
    {
        await using var onA = await a.ConnectAsync();
        await using var onB = await b.ConnectAsync();

        await a.Hub.Clients.All.SendAsync("Message", "hello all", TestContext.Current.CancellationToken);

        (await onA.NextAsync()).Should().Be("hello all");
        (await onB.NextAsync()).Should().Be("hello all");
    }

    [Fact]
    public async Task Should_skip_excluded_connections_on_every_server()
    {
        await using var onA = await a.ConnectAsync();
        await using var onB = await b.ConnectAsync();
        await using var alsoB = await b.ConnectAsync();

        await a.Hub.Clients.AllExcept([onB.ConnectionId]).SendAsync("Message", "not you", TestContext.Current.CancellationToken);

        (await onA.NextAsync()).Should().Be("not you");
        (await alsoB.NextAsync()).Should().Be("not you");
        await onB.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_send_to_a_connection_on_another_server()
    {
        await using var onB = await b.ConnectAsync();
        await using var otherOnB = await b.ConnectAsync();

        await a.Hub.Clients.Client(onB.ConnectionId).SendAsync("Message", "direct", TestContext.Current.CancellationToken);
        await a.Hub.Clients.Clients([otherOnB.ConnectionId]).SendAsync("Message", "direct too", TestContext.Current.CancellationToken);

        (await onB.NextAsync()).Should().Be("direct");
        (await otherOnB.NextAsync()).Should().Be("direct too");
        await onB.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_send_to_group_members_wherever_they_are()
    {
        await using var memberOnA = await a.ConnectAsync();
        await using var memberOnB = await b.ConnectAsync();
        await using var outsiderOnB = await b.ConnectAsync();
        await memberOnA.Connection.InvokeAsync("Join", "room", TestContext.Current.CancellationToken);
        await memberOnB.Connection.InvokeAsync("Join", "room", TestContext.Current.CancellationToken);

        await b.Hub.Clients.Group("room").SendAsync("Message", "to the room", TestContext.Current.CancellationToken);

        (await memberOnA.NextAsync()).Should().Be("to the room");
        (await memberOnB.NextAsync()).Should().Be("to the room");
        await outsiderOnB.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_stop_sending_after_leaving_a_group()
    {
        await using var member = await b.ConnectAsync();
        await member.Connection.InvokeAsync("Join", "room", TestContext.Current.CancellationToken);
        await member.Connection.InvokeAsync("Leave", "room", TestContext.Current.CancellationToken);

        await a.Hub.Clients.Group("room").SendAsync("Message", "gone", TestContext.Current.CancellationToken);

        await member.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_add_a_connection_on_another_server_to_a_group_with_an_ack()
    {
        await using var onB = await b.ConnectAsync();

        await a.Hub.Groups.AddToGroupAsync(onB.ConnectionId, "remote", TestContext.Current.CancellationToken);
        await a.Hub.Clients.Group("remote").SendAsync("Message", "joined remotely", TestContext.Current.CancellationToken);
        (await onB.NextAsync()).Should().Be("joined remotely");

        await a.Hub.Groups.RemoveFromGroupAsync(onB.ConnectionId, "remote", TestContext.Current.CancellationToken);
        await a.Hub.Clients.Group("remote").SendAsync("Message", "left remotely", TestContext.Current.CancellationToken);
        await onB.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_time_out_adding_a_connection_no_server_holds()
    {
        var add = () => a.Hub.Groups.AddToGroupAsync("no-such-connection", "room", TestContext.Current.CancellationToken);

        await add.Should().ThrowAsync<TimeoutException>().WithMessage("*no-such-connection*room*");
    }

    [Fact]
    public async Task Should_send_to_every_connection_of_a_user()
    {
        await using var aliceOnA = await a.ConnectAsync("alice");
        await using var aliceOnB = await b.ConnectAsync("alice");
        await using var bobOnB = await b.ConnectAsync("bob");

        await a.Hub.Clients.User("alice").SendAsync("Message", "hi alice", TestContext.Current.CancellationToken);
        await b.Hub.Clients.Users(["bob"]).SendAsync("Message", "hi bob", TestContext.Current.CancellationToken);

        (await aliceOnA.NextAsync()).Should().Be("hi alice");
        (await aliceOnB.NextAsync()).Should().Be("hi alice");
        (await bobOnB.NextAsync()).Should().Be("hi bob");
        await aliceOnA.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_send_to_several_groups_and_except_lists()
    {
        await using var red = await a.ConnectAsync();
        await using var blue = await b.ConnectAsync();
        await red.Connection.InvokeAsync("Join", "red", TestContext.Current.CancellationToken);
        await blue.Connection.InvokeAsync("Join", "blue", TestContext.Current.CancellationToken);

        await a.Hub.Clients.Groups(["red", "blue"]).SendAsync("Message", "both", TestContext.Current.CancellationToken);
        (await red.NextAsync()).Should().Be("both");
        (await blue.NextAsync()).Should().Be("both");

        await b.Hub.Clients.GroupExcept("red", [red.ConnectionId]).SendAsync("Message", "nobody", TestContext.Current.CancellationToken);
        await red.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_not_treat_group_names_as_wildcards()
    {
        await using var hashMember = await b.ConnectAsync();
        await using var starMember = await b.ConnectAsync();
        await hashMember.Connection.InvokeAsync("Join", "#", TestContext.Current.CancellationToken);
        await starMember.Connection.InvokeAsync("Join", "chat.*", TestContext.Current.CancellationToken);

        await a.Hub.Clients.Group("chat.private").SendAsync("Message", "private", TestContext.Current.CancellationToken);

        await hashMember.ShouldReceiveNothingAsync();
        await starMember.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_return_a_client_result_from_another_server()
    {
        await using var onB = await b.ConnectAsync();

        var sum = await a.Hub.Clients.Client(onB.ConnectionId).InvokeAsync<int>("Add", 2, 40, TestContext.Current.CancellationToken);

        sum.Should().Be(42);
    }

    [Fact]
    public async Task Should_return_a_client_result_from_the_same_server()
    {
        await using var onA = await a.ConnectAsync();

        var sum = await a.Hub.Clients.Client(onA.ConnectionId).InvokeAsync<int>("Add", 1, 1, TestContext.Current.CancellationToken);

        sum.Should().Be(2);
    }

    [Fact]
    public async Task Should_fail_a_client_result_for_a_connection_nobody_holds()
    {
        var invoke = () => a.Hub.Clients.Client("no-such-connection").InvokeAsync<int>("Add", 1, 2, TestContext.Current.CancellationToken);

        await invoke.Should().ThrowAsync<IOException>().WithMessage("*no-such-connection*does not exist*");
    }

    [Fact]
    public async Task Should_fail_a_remote_client_result_when_the_connection_drops()
    {
        var onB = await b.ConnectAsync();

        var invoke = a.Hub.Clients.Client(onB.ConnectionId).InvokeAsync<int>("Hang", 1, TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);
        await onB.DisposeAsync();

        var awaiting = () => invoke;
        await awaiting.Should().ThrowAsync<HubException>().WithMessage("*disconnected*");
    }

    [Fact]
    public async Task Should_cancel_a_pending_client_result()
    {
        await using var onB = await b.ConnectAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        var invoke = () => a.Hub.Clients.Client(onB.ConnectionId).InvokeAsync<int>("Hang", 1, cts.Token);

        await invoke.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_unbind_a_connections_keys_on_disconnect()
    {
        var client = await b.ConnectAsync("carol");
        await client.Connection.InvokeAsync("Join", "room", TestContext.Current.CancellationToken);
        var names = b.Backplane.Names;
        Bound(names.Exchange, names.Queue).Should().Contain(["conn." + client.ConnectionId, "user.carol", "group.room"]);

        await client.DisposeAsync();
        await WaitUntilAsync(() => !Bound(names.Exchange, names.Queue).Contains("conn." + client.ConnectionId));

        Bound(names.Exchange, names.Queue).Should().NotContain(["user.carol", "group.room"]);
        Bound(names.Exchange, names.Queue).Should().Contain(["all", "groups", names.OwnAck, names.OwnReturn]);
    }

    [Fact]
    public async Task Should_redeclare_its_queue_and_bindings_when_the_broker_cancels_its_consumer()
    {
        await using var member = await b.ConnectAsync("dave");
        await member.Connection.InvokeAsync("Join", "room", TestContext.Current.CancellationToken);
        var names = b.Backplane.Names;

        // the server queue deleted under the running consumer, bindings and all: no manual resync, the lifecycle
        // Cancelled event alone must bring it back
        foreach (var key in Bound(names.Exchange, names.Queue))
            broker.Unbind(new BindingDefinition(names.Exchange, names.Queue, key));
        broker.DeleteQueue(names.Queue);
        await WaitUntilAsync(() => Bound(names.Exchange, names.Queue).Contains("group.room"));

        await a.Hub.Clients.Group("room").SendAsync("Message", "after recovery", TestContext.Current.CancellationToken);
        (await member.NextAsync()).Should().Be("after recovery");
        await a.Hub.Clients.User("dave").SendAsync("Message", "user too", TestContext.Current.CancellationToken);
        (await member.NextAsync()).Should().Be("user too");
    }

    [Fact]
    public async Task Should_delete_its_queue_on_shutdown()
    {
        await using var c = await TestServer.StartInMemoryAsync(broker, "server-c");
        await using (await c.ConnectAsync())
        {
        }
        var queue = c.Backplane.Names.Queue;
        broker.QueueExists(queue).Should().BeTrue();

        await c.DisposeAsync();

        broker.QueueExists(queue).Should().BeFalse();
    }

    private string[] Bound(string exchange, string queue)
        => broker.Exchanges.TryGetValue(exchange, out var e)
            ? e.Bindings.Where(x => x.Destination == queue).Select(x => x.RoutingKey).ToArray()
            : [];

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(50);
        condition().Should().BeTrue();
    }
}
