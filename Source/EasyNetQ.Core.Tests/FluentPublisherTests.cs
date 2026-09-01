using EasyNetQ.Configuration;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public class FluentPublisherTests
{
    public sealed record OrderPlaced(int Id, string Product);
    public sealed record OrderShipped(int Id);

    private static ServiceCollection CreateServices(InMemoryTransport transport)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(transport);
        return services;
    }

    [Fact]
    public async Task Should_publish_via_registered_route()
    {
        var received = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);

        var transport = new InMemoryTransport();
        var services = CreateServices(transport);
        services.AddEasyNetQCore()
            .Publish(publish => publish
                .Exchange("orders")
                .Message<OrderPlaced>("order.placed")
            )
            .Consume(consumer => consumer
                .Queue("orders.billing")
                .Bind("orders", "order.*")
                .Handle<OrderPlaced>((order, _) =>
                {
                    received.TrySetResult(order);
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                })
            );

        await using var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);

        var publisher = provider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new OrderPlaced(7, "socks"), TestContext.Current.CancellationToken);

        var order = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        order.Should().Be(new OrderPlaced(7, "socks"));

        await hostedService.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_resolve_routing_key_per_message()
    {
        var received = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);

        var transport = new InMemoryTransport();
        var services = CreateServices(transport);
        services.AddEasyNetQCore()
            .Publish(publish => publish
                .Exchange("orders")
                .Message<OrderPlaced>(order => $"order.{order.Product}")
            )
            .Consume(consumer => consumer
                .Queue("orders.socks")
                .Bind("orders", "order.socks")
                .Handle<OrderPlaced>((order, _) =>
                {
                    received.TrySetResult(order);
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                })
            );

        await using var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);

        var publisher = provider.GetRequiredService<IMessagePublisher>();
        // routed away: no binding matches order.shoes
        await publisher.PublishAsync(new OrderPlaced(1, "shoes"), TestContext.Current.CancellationToken);
        await publisher.PublishAsync(new OrderPlaced(2, "socks"), TestContext.Current.CancellationToken);

        var order = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        order.Id.Should().Be(2);

        await hostedService.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_throw_for_unrouted_message_type()
    {
        var transport = new InMemoryTransport();
        var services = CreateServices(transport);
        services.AddEasyNetQCore()
            .Publish(publish => publish.Exchange("orders").Message<OrderPlaced>("order.placed"));

        await using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMessagePublisher>();

        var publish = async () => await publisher.PublishAsync(new OrderShipped(1), TestContext.Current.CancellationToken);
        await publish.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No publish route*OrderShipped*");
    }

    [Fact]
    public async Task Should_throw_when_type_is_routed_twice()
    {
        var transport = new InMemoryTransport();
        var services = CreateServices(transport);
        services.AddEasyNetQCore()
            .Publish(publish => publish.Exchange("orders").Message<OrderPlaced>("a"))
            .Publish(publish => publish.Exchange("audit").Message<OrderPlaced>("b"));

        await using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMessagePublisher>();

        var publish = async () => await publisher.PublishAsync(new OrderPlaced(1, "socks"), TestContext.Current.CancellationToken);
        await publish.Should().ThrowAsync<InvalidOperationException>().WithMessage("*more than one publish definition*");
    }

    [Fact]
    public async Task Should_run_custom_pipeline_step_after_serialization()
    {
        var bodyLengthSeen = -1;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var transport = new InMemoryTransport();
        var services = CreateServices(transport);
        services.AddEasyNetQCore()
            .Publish(publish => publish
                .Exchange("orders")
                .Message<OrderPlaced>("order.placed")
                .Pipeline(pipeline => pipeline.Use("probe", (context, next) =>
                {
                    bodyLengthSeen = context.Body.Length;
                    return next(context);
                }))
            )
            .Consume(consumer => consumer
                .Queue("orders.billing")
                .Bind("orders", "order.placed")
                .Handle<OrderPlaced>((_, _) =>
                {
                    received.TrySetResult();
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                })
            );

        await using var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);

        var publisher = provider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new OrderPlaced(7, "socks"), TestContext.Current.CancellationToken);

        await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        bodyLengthSeen.Should().BeGreaterThan(0);

        await hostedService.StopAsync(TestContext.Current.CancellationToken);
    }
}
