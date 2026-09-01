using EasyNetQ.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EasyNetQ.Tests.Mocking;

namespace EasyNetQ.Tests;

public class FluentBuilderTests
{
    private sealed class TestBuilder(IServiceCollection services) : IEasyNetQBuilder
    {
        public IServiceCollection Services { get; } = services;
    }

    [Fact]
    public async Task Should_declare_typed_queue_and_start_consumer()
    {
        await using var mockBuilder = new MockBuilder(x =>
            new TestBuilder(x).UseRabbitMq(rabbit => rabbit
                .Consume(consumer => consumer
                    .Queue("typed.q", queue => queue.Quorum().DeadLetterExchange("typed.dlx").MessageTtl(TimeSpan.FromMinutes(5)))
                    .Bind("orders", "order.*", exchange => exchange.Topic().Durable())
                    .ConsumerTag("billing-1")
                    .Handle<MyMessage>((_, _) => new ValueTask<AckDecision>(AckDecision.Ack))
                )
            )
        );

        var hostedService = mockBuilder.ServiceProvider.GetServices<IHostedService>().Single();
        await hostedService.StartAsync(TestContext.Current.CancellationToken);

        var calls = mockBuilder.Channels.SelectMany(c => c.ReceivedCalls()).ToList();

        var queueDeclare = calls.Single(c => c.GetMethodInfo().Name == "QueueDeclareAsync" && Equals(c.GetArguments()[0], "typed.q"));
        var arguments = (IDictionary<string, object>)queueDeclare.GetArguments()[4]!;
        arguments["x-queue-type"].Should().Be("quorum");
        arguments["x-dead-letter-exchange"].Should().Be("typed.dlx");
        arguments["x-message-ttl"].Should().Be(300000);

        calls.Should().Contain(c => c.GetMethodInfo().Name == "ExchangeDeclareAsync" && Equals(c.GetArguments()[0], "orders"));
        calls.Should().Contain(c => c.GetMethodInfo().Name == "QueueBindAsync" && Equals(c.GetArguments()[0], "typed.q") && Equals(c.GetArguments()[1], "orders"));

        var consume = calls.Single(c => c.GetMethodInfo().Name == "BasicConsumeAsync");
        consume.GetArguments()[0].Should().Be("typed.q");
        consume.GetArguments()[2].Should().Be("billing-1");

        await hostedService.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_declare_typed_exchange_and_publish_via_route()
    {
        await using var mockBuilder = new MockBuilder(x =>
            new TestBuilder(x).UseRabbitMq(rabbit => rabbit
                .Publish(publish => publish
                    .Exchange("orders", exchange => exchange.Topic().AlternateExchange("orders.alt"))
                    .Message<MyMessage>("order.placed")
                )
            )
        );

        var publisher = mockBuilder.ServiceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new MyMessage { Text = "hello" }, TestContext.Current.CancellationToken);

        var calls = mockBuilder.Channels.SelectMany(c => c.ReceivedCalls()).ToList();

        var exchangeDeclare = calls.Single(c => c.GetMethodInfo().Name == "ExchangeDeclareAsync" && Equals(c.GetArguments()[0], "orders"));
        exchangeDeclare.GetArguments()[1].Should().Be("topic");
        var arguments = (IDictionary<string, object>)exchangeDeclare.GetArguments()[4]!;
        arguments["alternate-exchange"].Should().Be("orders.alt");

        var publish = calls.Single(c => c.GetMethodInfo().Name == "BasicPublishAsync");
        publish.GetArguments()[0].Should().Be("orders");
        publish.GetArguments()[1].Should().Be("order.placed");
    }
}
