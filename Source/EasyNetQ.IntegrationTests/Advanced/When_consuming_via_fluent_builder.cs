using EasyNetQ.Configuration;
using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_consuming_via_fluent_builder(RabbitMQFixture rmqFixture)
{
    public sealed record FluentOrder(int Id);

    [Fact]
    public async Task Should_declare_typed_queue_and_consume()
    {
        var queueName = $"fluent-{Guid.NewGuid():N}";
        var received = new TaskCompletionSource<FluentOrder>(TaskCreationOptions.RunContinuationsAsynchronously);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host}")
            .UseRabbitMq(rabbit => rabbit
                .Consume(consumer => consumer
                    .Queue(queueName, queue => queue.Quorum())
                    .Handle<FluentOrder>((order, _) =>
                    {
                        received.TrySetResult(order);
                        return new ValueTask<AckDecision>(AckDecision.Ack);
                    })
                )
            );

        await using var provider = serviceCollection.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var bus = provider.GetRequiredService<IBus>();
            await bus.Advanced.PublishAsync<FluentOrder>(
                "", queueName, null, null, default, new FluentOrder(42), TestContext.Current.CancellationToken
            );

            var order = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            order.Should().Be(new FluentOrder(42));
        }
        finally
        {
            await hostedService.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Should_publish_via_fluent_route_and_consume()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var exchangeName = $"fluent-orders-{suffix}";
        var queueName = $"fluent-pub-{suffix}";
        var received = new TaskCompletionSource<FluentOrder>(TaskCreationOptions.RunContinuationsAsynchronously);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host}")
            .UseRabbitMq(rabbit => rabbit
                .Publish(publish => publish
                    .Exchange(exchangeName, exchange => exchange.Topic().AutoDelete())
                    .Message<FluentOrder>(order => $"order.{order.Id}")
                )
                .Consume(consumer => consumer
                    .Queue(queueName, queue => queue.AutoDelete())
                    .Bind(exchangeName, "order.*", exchange => exchange.Topic().AutoDelete())
                    .Handle<FluentOrder>((order, _) =>
                    {
                        received.TrySetResult(order);
                        return new ValueTask<AckDecision>(AckDecision.Ack);
                    })
                )
            );

        await using var provider = serviceCollection.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var publisher = provider.GetRequiredService<IMessagePublisher>();
            await publisher.PublishAsync(new FluentOrder(42), TestContext.Current.CancellationToken);

            var order = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            order.Should().Be(new FluentOrder(42));
        }
        finally
        {
            await hostedService.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
