using System.Text.Json.Serialization;
using System.Threading.Channels;
using EasyNetQ;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

// Native AOT smoke test of the SignalR backplane: two servers in one process share a RabbitMQ, a client on server A
// receives what server B sends (broadcast, cross-server group, client result). Exit code 0 = every check passed.
var connectionString = args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("EASYNETQ_SIGNALR_RABBITMQ") ?? "host=localhost";

await using var a = await ChatServer.StartAsync(connectionString, "aot-a");
await using var b = await ChatServer.StartAsync(connectionString, "aot-b");

var received = Channel.CreateUnbounded<string>();
await using var client = new HubConnectionBuilder()
    .WithUrl($"{a.Url}/chat")
    .AddJsonProtocol(o => o.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, ChatJson.Default))
    .Build();
client.On<string>("Message", message => received.Writer.TryWrite(message));
client.On<int, int, int>("Add", (x, y) => x + y);
await client.StartAsync();
var connectionId = await client.InvokeAsync<string>("WhoAmI");

var failures = 0;
async Task CheckAsync(string name, Func<Task<bool>> check)
{
    try
    {
        var passed = await check().WaitAsync(TimeSpan.FromSeconds(15));
        Console.WriteLine($"{(passed ? "ok  " : "FAIL")} {name}");
        if (!passed) failures++;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"FAIL {name}: {exception.GetType().Name} {exception.Message}");
        failures++;
    }
}

await CheckAsync("broadcast from the other server", async () =>
{
    await b.Hub.Clients.All.SendAsync("Message", "all");
    return await received.Reader.ReadAsync() == "all";
});
await CheckAsync("cross-server group with ack", async () =>
{
    await b.Hub.Groups.AddToGroupAsync(connectionId, "room");
    await b.Hub.Clients.Group("room").SendAsync("Message", "room");
    return await received.Reader.ReadAsync() == "room";
});
await CheckAsync("client result through the backplane", async () =>
    await b.Hub.Clients.Client(connectionId).InvokeAsync<int>("Add", 40, 2, CancellationToken.None) == 42);

return failures == 0 ? 0 : 1;

public sealed class ChatHub : Hub
{
    public string WhoAmI() => Context.ConnectionId;
}

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
internal sealed partial class ChatJson : JsonSerializerContext;

internal sealed class ChatServer : IAsyncDisposable
{
    private readonly WebApplication app;

    private ChatServer(WebApplication app, string url)
    {
        this.app = app;
        Url = url;
    }

    public string Url { get; }
    public IHubContext<ChatHub> Hub => app.Services.GetRequiredService<IHubContext<ChatHub>>();

    public static async Task<ChatServer> StartAsync(string connectionString, string serverName)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddEasyNetQ(connectionString);
        builder.Services.AddSignalR()
            .AddJsonProtocol(o => o.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, ChatJson.Default))
            .AddEasyNetQ(backplane => backplane.Prefix("signalr-aot").ServerName(serverName));
        var app = builder.Build();
        app.MapHub<ChatHub>("/chat");
        await app.StartAsync();
        var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new ChatServer(app, url);
    }

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}
