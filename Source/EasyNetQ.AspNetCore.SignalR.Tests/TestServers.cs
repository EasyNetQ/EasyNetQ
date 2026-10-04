using System.Threading.Channels;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.AspNetCore.SignalR.Tests;

public sealed class ChatHub : Hub
{
    public Task Join(string group) => Groups.AddToGroupAsync(Context.ConnectionId, group);

    public Task Leave(string group) => Groups.RemoveFromGroupAsync(Context.ConnectionId, group);

    public string WhoAmI() => Context.ConnectionId;
}

public sealed class QueryUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.GetHttpContext()?.Request.Query["user"].FirstOrDefault();
}

/// <summary>One SignalR server: real Kestrel, real hub pipeline, the backplane on a shared transport</summary>
public sealed class TestServer : IAsyncDisposable
{
    private readonly WebApplication app;

    private TestServer(WebApplication app, string url)
    {
        this.app = app;
        Url = url;
    }

    public string Url { get; }
    public IServiceProvider Services => app.Services;
    public IHubContext<ChatHub> Hub => app.Services.GetRequiredService<IHubContext<ChatHub>>();
    public EasyNetQHubLifetimeManager<ChatHub> Backplane => (EasyNetQHubLifetimeManager<ChatHub>)app.Services.GetRequiredService<HubLifetimeManager<ChatHub>>();

    public static Task<TestServer> StartInMemoryAsync(InMemoryBroker broker, string name, TimeSpan? ackTimeout = null)
        => StartAsync(name, services =>
        {
            services.AddSingleton<ITransport>(new InMemoryTransport(broker));
            services.AddEasyNetQCore();
        }, ackTimeout);

    public static async Task<TestServer> StartAsync(string name, Action<IServiceCollection> addTransport, TimeSpan? ackTimeout = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        if (Environment.GetEnvironmentVariable("EASYNETQ_SIGNALR_TEST_LOGS") is not null)
            builder.Logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Debug);
        addTransport(builder.Services);
        builder.Services.AddSingleton<IUserIdProvider, QueryUserIdProvider>();
        builder.Services.AddSignalR().AddEasyNetQ(b => b.ServerName(name).AckTimeout(ackTimeout ?? TimeSpan.FromSeconds(5)));
        var app = builder.Build();
        app.MapHub<ChatHub>("/chat");
        await app.StartAsync();
        var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new TestServer(app, url);
    }

    public async Task<TestClient> ConnectAsync(string? user = null)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(user is null ? $"{Url}/chat" : $"{Url}/chat?user={Uri.EscapeDataString(user)}")
            .Build();
        var client = new TestClient(connection);
        await connection.StartAsync();
        client.ConnectionId = await connection.InvokeAsync<string>("WhoAmI");
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}

public sealed class TestClient : IAsyncDisposable
{
    private readonly Channel<string> received = Channel.CreateUnbounded<string>();

    public TestClient(HubConnection connection)
    {
        Connection = connection;
        connection.On<string>("Message", message => received.Writer.TryWrite(message));
        connection.On<int, int, int>("Add", (a, b) => a + b);
        connection.On<int, int>("Hang", async _ =>
        {
            await Task.Delay(Timeout.Infinite);
            return 0;
        });
    }

    public HubConnection Connection { get; }
    public string ConnectionId { get; set; } = "";

    public async Task<string> NextAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
        return await received.Reader.ReadAsync(cts.Token);
    }

    /// <summary>Nothing arrives within the window (backplane deliveries take milliseconds here)</summary>
    public async Task ShouldReceiveNothingAsync(TimeSpan? window = null)
    {
        await Task.Delay(window ?? TimeSpan.FromMilliseconds(500));
        received.Reader.TryRead(out var unexpected).Should().BeFalse($"no message was expected, got '{unexpected}'");
    }

    public ValueTask DisposeAsync() => Connection.DisposeAsync();
}
