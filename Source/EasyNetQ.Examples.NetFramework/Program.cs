using EasyNetQ;
using EasyNetQ.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Examples.NetFramework;

// Minimal .NET Framework consumer: legacy facade plus the fluent v9 API, built against the
// netstandard2.0 assets. Usage: EasyNetQ.Examples.NetFramework [connectionString]
public static class Program
{
    public sealed class Ping
    {
        public Guid Id { get; set; }
    }

    public static async Task Main(string[] args)
    {
        var connectionString = args.Length > 0 ? args[0] : "host=localhost";

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddEasyNetQ(connectionString)
            .UseRabbitMq(rabbit => rabbit
                .Publish(publish => publish
                    .Exchange("netfx-pings", exchange => exchange.Topic().AutoDelete())
                    .Message<Ping>("ping")
                )
                .Consume(consumer => consumer
                    .Queue("netfx-pings", queue => queue.AutoDelete())
                    .Bind("netfx-pings", "ping", exchange => exchange.Topic().AutoDelete())
                    .Handle<Ping>((ping, _) =>
                    {
                        Console.WriteLine($"received {ping.Id}");
                        return new ValueTask<AckDecision>(AckDecision.Ack);
                    })
                )
            );

        using (var provider = services.BuildServiceProvider())
        {
            foreach (var hostedService in provider.GetServices<IHostedService>())
                await hostedService.StartAsync(CancellationToken.None);

            var publisher = provider.GetRequiredService<IMessagePublisher>();
            await publisher.PublishAsync(new Ping { Id = Guid.NewGuid() });

            await Task.Delay(1000);

            foreach (var hostedService in provider.GetServices<IHostedService>())
                await hostedService.StopAsync(CancellationToken.None);
        }
    }
}
