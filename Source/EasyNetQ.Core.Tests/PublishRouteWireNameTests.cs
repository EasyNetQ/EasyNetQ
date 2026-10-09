using EasyNetQ.Hosting;
using EasyNetQ.Configuration;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public class PublishRouteWireNameTests
{
    public sealed record OrderShipped(int Id);

    [Fact]
    public async Task Should_stamp_a_route_specific_wire_name()
    {
        var received = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new InMemoryTransport();
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(transport);
        services.AddEasyNetQCore()
            .MessageType<OrderShipped>(m => m.Alias("shipping.shipped.v1"))
            .Publish(p => p.Exchange("shipping").Message<OrderShipped>("shipped", r => r.WireName("shipping.shipped.v1")))
            .Consume(c => c
                .Queue("shipping.audit")
                .Bind("shipping", "#")
                .Handle<OrderShipped>((_, context) =>
                {
                    received.TrySetResult(context.Properties.Type);
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                })
            );

        await using var provider = services.BuildServiceProvider();
        var host = provider.GetServices<IHostedService>().Single();
        await host.StartAsync(TestContext.Current.CancellationToken);
        await provider.GetRequiredService<IConsumerHostStatus>().WaitForStartedAsync(TestContext.Current.CancellationToken);

        await provider.GetRequiredService<IMessagePublisher>().PublishAsync(new OrderShipped(3), TestContext.Current.CancellationToken);

        var type = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        type.Should().Be("shipping.shipped.v1");
        await host.StopAsync(TestContext.Current.CancellationToken);
    }
}
