using System.Text.Json;
using EasyNetQ.Configuration;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public class WireNameTests
{
    public sealed record OrderPlaced(int Id, string Product);
    public sealed record OrderShipped(int Id);

    [Fact]
    public void Should_register_explicit_wire_names_and_aliases()
    {
        var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());

        registry.Register<OrderPlaced>("orders.placed.v1", ["Legacy.OrderPlaced"]);

        registry.GetOrAdd<OrderPlaced>().WireName.Should().Be("orders.placed.v1");
        registry.GetByWireName("orders.placed.v1").Type.Should().Be<OrderPlaced>();
        registry.GetByWireName("Legacy.OrderPlaced").Type.Should().Be<OrderPlaced>();
    }

    [Fact]
    public void Should_reject_a_wire_name_that_resolves_to_two_types()
    {
        static MessageTypeRegistry Registry()
        {
            var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());
            registry.Register<OrderPlaced>("orders.placed.v1");
            return registry;
        }

        var rename = () => Registry().Register<OrderPlaced>("orders.placed.v2");
        var aliasTaken = () => Registry().Register<OrderShipped>(null, ["orders.placed.v1"]);
        var nameTaken = () => Registry().Register<OrderShipped>("orders.placed.v1");

        rename.Should().Throw<EasyNetQException>().WithMessage("*already registered with wire name 'orders.placed.v1'*");
        aliasTaken.Should().Throw<EasyNetQException>().WithMessage("*'orders.placed.v1' already resolves to*");
        nameTaken.Should().Throw<EasyNetQException>().WithMessage("*'orders.placed.v1' already resolves to*");
    }

    [Fact]
    public void Should_apply_configured_mappings_before_generated_initializers()
    {
        var services = new ServiceCollection();
        // the generated module registers its initializer when AddEasyNetQ runs, i.e. before user configuration
        services.AddSingleton<IMessageTypeRegistryInitializer>(new GetOrAddInitializer());
        services.AddEasyNetQCore().MessageType<OrderPlaced>(m => m.WireName("orders.placed.v1"));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMessageTypeRegistry>().GetOrAdd<OrderPlaced>().WireName.Should().Be("orders.placed.v1");
    }

    [Fact]
    public async Task Should_publish_and_match_configured_wire_names()
    {
        var received = new TaskCompletionSource<(OrderPlaced Order, string? Type)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new InMemoryTransport();
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(transport);
        services.AddEasyNetQCore()
            .MessageType<OrderPlaced>(m => m.WireName("orders.placed.v1").Alias("Legacy.OrderPlaced"))
            .Publish(p => p.Exchange("orders").Message<OrderPlaced>("order.placed"))
            .Consume(c => c
                .Queue("orders.billing")
                .Bind("orders", "order.#")
                .Handle<OrderPlaced>((order, context) =>
                {
                    received.TrySetResult((order, context.Properties.Type));
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                })
            );

        await using var provider = services.BuildServiceProvider();
        var host = provider.GetServices<IHostedService>().Single();
        await host.StartAsync(TestContext.Current.CancellationToken);

        await provider.GetRequiredService<IMessagePublisher>().PublishAsync(new OrderPlaced(1, "socks"), TestContext.Current.CancellationToken);
        var published = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        published.Should().Be((new OrderPlaced(1, "socks"), "orders.placed.v1"));

        received = new TaskCompletionSource<(OrderPlaced Order, string? Type)>(TaskCreationOptions.RunContinuationsAsynchronously);
        await PublishRawAsync(transport, provider, "orders", "order.legacy", "Legacy.OrderPlaced", new OrderPlaced(2, "hats"));
        var aliased = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        aliased.Should().Be((new OrderPlaced(2, "hats"), "Legacy.OrderPlaced"));

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    internal static async Task PublishRawAsync<T>(InMemoryTransport transport, IServiceProvider provider, string exchange, string routingKey, string? type, T body)
    {
        var connectionContext = new ConnectionContext("producer", provider);
        var connection = await transport.ConnectAsync(connectionContext, TestContext.Current.CancellationToken);
        var channel = await connection.OpenChannelAsync(new ChannelContext(connectionContext), TestContext.Current.CancellationToken);
        await channel.PublishAsync(new PublishContext(new ChannelContext(connectionContext))
        {
            Exchange = exchange,
            RoutingKey = routingKey,
            Properties = new MessageProperties { Type = type },
            Body = JsonSerializer.SerializeToUtf8Bytes(body),
            CancellationToken = TestContext.Current.CancellationToken
        });
    }

    private sealed class GetOrAddInitializer : IMessageTypeRegistryInitializer
    {
        public void Initialize(IMessageTypeRegistry registry) => registry.GetOrAdd<OrderPlaced>();
    }
}
