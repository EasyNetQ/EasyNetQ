using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.AspNetCore.SignalR.Tests;

/// <summary>
///     The same scale-out semantics against a real RabbitMQ. Runs when EASYNETQ_SIGNALR_RABBITMQ holds an EasyNetQ
///     connection string (use a throwaway vhost); the recovery test also needs EASYNETQ_SIGNALR_RABBITMQ_MANAGEMENT
///     (<c>http://user:password@host:15672</c>) to drop the vhost's connections like a broker restart would.
/// </summary>
public sealed class When_scaling_out_over_rabbitmq : IAsyncLifetime
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("EASYNETQ_SIGNALR_RABBITMQ");
    private static readonly string? Management = Environment.GetEnvironmentVariable("EASYNETQ_SIGNALR_RABBITMQ_MANAGEMENT");
    private TestServer? a;
    private TestServer? b;

    public async ValueTask InitializeAsync()
    {
        if (ConnectionString is null) return;
        a = await TestServer.StartAsync("rabbit-a", services => services.AddEasyNetQ(ConnectionString), TimeSpan.FromSeconds(5));
        b = await TestServer.StartAsync("rabbit-b", services => services.AddEasyNetQ(ConnectionString), TimeSpan.FromSeconds(5));
    }

    public async ValueTask DisposeAsync()
    {
        if (a is not null) await a.DisposeAsync();
        if (b is not null) await b.DisposeAsync();
    }

    [Fact]
    public async Task Should_fan_out_to_all_groups_users_and_connections()
    {
        Assert.SkipWhen(ConnectionString is null, "EASYNETQ_SIGNALR_RABBITMQ not set");
        var ct = TestContext.Current.CancellationToken;
        await using var aliceOnA = await a!.ConnectAsync("alice");
        await using var aliceOnB = await b!.ConnectAsync("alice");
        await using var bobOnB = await b.ConnectAsync("bob");

        await a.Hub.Clients.All.SendAsync("Message", "all", ct);
        (await aliceOnA.NextAsync()).Should().Be("all");
        (await aliceOnB.NextAsync()).Should().Be("all");
        (await bobOnB.NextAsync()).Should().Be("all");

        await a.Hub.Groups.AddToGroupAsync(bobOnB.ConnectionId, "room", ct);
        await a.Hub.Clients.Group("room").SendAsync("Message", "room", ct);
        (await bobOnB.NextAsync()).Should().Be("room");

        await b.Hub.Clients.User("alice").SendAsync("Message", "alice", ct);
        (await aliceOnA.NextAsync()).Should().Be("alice");
        (await aliceOnB.NextAsync()).Should().Be("alice");

        await a.Hub.Clients.Client(bobOnB.ConnectionId).SendAsync("Message", "bob", ct);
        (await bobOnB.NextAsync()).Should().Be("bob");
        await aliceOnA.ShouldReceiveNothingAsync();
    }

    [Fact]
    public async Task Should_route_client_results_and_report_missing_connections()
    {
        Assert.SkipWhen(ConnectionString is null, "EASYNETQ_SIGNALR_RABBITMQ not set");
        var ct = TestContext.Current.CancellationToken;
        await using var onB = await b!.ConnectAsync();

        (await a!.Hub.Clients.Client(onB.ConnectionId).InvokeAsync<int>("Add", 20, 22, ct)).Should().Be(42);

        // mandatory + publisher confirm: the broker returns the invocation when no server holds the connection
        var missing = () => a.Hub.Clients.Client("no-such-connection").InvokeAsync<int>("Add", 1, 2, ct);
        await missing.Should().ThrowAsync<IOException>().WithMessage("*does not exist*");
    }

    [Fact]
    public async Task Should_redeclare_its_queue_when_the_broker_deletes_it()
    {
        Assert.SkipWhen(ConnectionString is null || Management is null, "EASYNETQ_SIGNALR_RABBITMQ(_MANAGEMENT) not set");
        var ct = TestContext.Current.CancellationToken;
        await using var member = await b!.ConnectAsync("erin");
        await member.Connection.InvokeAsync("Join", "room", ct);

        // an operator, a policy or a lost queue node: the broker cancels the consumer, the connection stays up
        using var management = ManagementClient(out var vhost);
        (await management.DeleteAsync($"api/queues/{vhost}/{Uri.EscapeDataString(b.Backplane.Names.Queue)}", ct)).EnsureSuccessStatusCode();

        (await DeliverAsync(member, "after queue loss", ct)).Should().Be("after queue loss");
    }

    [Fact]
    public async Task Should_recover_after_the_broker_drops_its_connections()
    {
        Assert.SkipWhen(ConnectionString is null || Management is null, "EASYNETQ_SIGNALR_RABBITMQ(_MANAGEMENT) not set");
        var ct = TestContext.Current.CancellationToken;
        await using var member = await b!.ConnectAsync("frank");
        await member.Connection.InvokeAsync("Join", "room", ct);

        // the management API lists connections a few seconds late
        using var management = ManagementClient(out var vhost);
        List<Dictionary<string, object>> connections = [];
        for (var attempt = 0; attempt < 30 && connections.Count < 4; attempt++)
        {
            connections = (await management.GetFromJsonAsync<List<Dictionary<string, object>>>($"api/vhosts/{vhost}/connections", ct))!;
            if (connections.Count < 4) await Task.Delay(1000, ct);
        }
        connections.Should().NotBeEmpty();
        foreach (var connection in connections)
            await management.DeleteAsync($"api/connections/{Uri.EscapeDataString(connection["name"].ToString()!)}", ct);

        (await DeliverAsync(member, "after reconnect", ct)).Should().Be("after reconnect");
    }

    private async Task<string?> DeliverAsync(TestClient member, string message, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                await a!.Hub.Clients.Group("room").SendAsync("Message", message, ct);
                return await member.NextAsync(TimeSpan.FromSeconds(2));
            }
            catch (Exception exception) when (exception is OperationCanceledException or EasyNetQException)
            {
                // still recovering
            }
        }
        return null;
    }

    private static HttpClient ManagementClient(out string vhost)
    {
        var uri = new Uri(Management!);
        var client = new HttpClient { BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority) + "/") };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.UnescapeDataString(uri.UserInfo)))
        );
        var virtualHost = ConnectionString!.Split(';')
            .Select(part => part.Split('=', 2))
            .FirstOrDefault(pair => pair[0].Trim().Equals("virtualHost", StringComparison.OrdinalIgnoreCase))?[1] ?? "/";
        vhost = Uri.EscapeDataString(virtualHost);
        return client;
    }
}
