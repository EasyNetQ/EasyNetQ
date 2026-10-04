using System.Text.Json.Serialization;
using EasyNetQ;
using EasyNetQ.Configuration;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddEasyNetQ("host=localhost")
    .UseSystemTextJson(AppJsonContext.Default)
    .UseRabbitMq(r => r.Publish(p => p.ExistingExchange("app.events").Message<Ping>("ping")));
builder.Build().Run();

public sealed record Ping(string Text);

[JsonSerializable(typeof(Ping))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
