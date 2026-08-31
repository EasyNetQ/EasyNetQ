using System.Text;
using System.Text.Json;
using EasyNetQ.Configuration;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public class FluentConsumerTests
{
    public sealed record OrderPlaced(int Id, string Product);

    [Fact]
    public async Task Should_consume_typed_messages_registered_fluently()
    {
        var received = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);

        var services = new ServiceCollection();
        var transport = new InMemoryTransport();
        services.AddSingleton<ITransport>(transport);
        services.AddEasyNetQCore()
            .Consume(consumer => consumer
                .Queue("orders.billing")
                .Bind("orders", "order.*")
                .PrefetchCount(16)
                .Handle<OrderPlaced>((order, context) =>
                {
                    received.TrySetResult(order);
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                })
            );

        await using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        hostedServices.Should().ContainSingle();
        await hostedServices[0].StartAsync(TestContext.Current.CancellationToken);

        // publish straight through the transport, the way a remote producer would
        var registry = provider.GetRequiredService<IMessageTypeRegistry>();
        var wireName = registry.GetOrAdd<OrderPlaced>().WireName;
        var connectionContext = new ConnectionContext("producer", provider);
        var connection = await transport.ConnectAsync(connectionContext, TestContext.Current.CancellationToken);
        var channel = await connection.OpenChannelAsync(new ChannelContext(connectionContext), TestContext.Current.CancellationToken);
        await channel.PublishAsync(new PublishContext(new ChannelContext(connectionContext))
        {
            Exchange = "orders",
            RoutingKey = "order.created",
            Properties = new MessageProperties { Type = wireName },
            Body = JsonSerializer.SerializeToUtf8Bytes(new OrderPlaced(7, "socks")),
            CancellationToken = TestContext.Current.CancellationToken
        });

        var order = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        order.Should().Be(new OrderPlaced(7, "socks"));

        await hostedServices[0].StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_apply_custom_message_middleware()
    {
        var order = new List<string>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var services = new ServiceCollection();
        var transport = new InMemoryTransport();
        services.AddSingleton<ITransport>(transport);
        services.AddEasyNetQCore()
            .Consume(consumer => consumer
                .Queue("middleware.q")
                .Handle<OrderPlaced>((_, _) =>
                {
                    order.Add("handler");
                    received.TrySetResult();
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                })
                .Message(pipeline => pipeline.Use("custom", (context, next) =>
                {
                    order.Add("middleware");
                    return next(context);
                }))
            );

        await using var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);

        var registry = provider.GetRequiredService<IMessageTypeRegistry>();
        var connectionContext = new ConnectionContext("producer", provider);
        var connection = await transport.ConnectAsync(connectionContext, TestContext.Current.CancellationToken);
        var channel = await connection.OpenChannelAsync(new ChannelContext(connectionContext), TestContext.Current.CancellationToken);
        await channel.PublishAsync(new PublishContext(new ChannelContext(connectionContext))
        {
            Exchange = "",
            RoutingKey = "middleware.q",
            Properties = new MessageProperties { Type = registry.GetOrAdd<OrderPlaced>().WireName },
            Body = JsonSerializer.SerializeToUtf8Bytes(new OrderPlaced(1, "x")),
            CancellationToken = TestContext.Current.CancellationToken
        });

        await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        order.Should().Equal("middleware", "handler");
        await hostedService.StopAsync(TestContext.Current.CancellationToken);
    }
}
