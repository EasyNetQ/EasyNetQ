using System.Text;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Core.Tests;

public class InMemoryTransportTests
{
    private static async Task<(ITransportChannel Channel, InMemoryTransport Transport)> OpenChannelAsync(IServiceProvider services)
    {
        var transport = new InMemoryTransport();
        var connectionContext = new ConnectionContext("test", services);
        var connection = await transport.ConnectAsync(connectionContext, TestContext.Current.CancellationToken);
        var channel = await connection.OpenChannelAsync(new ChannelContext(connectionContext), TestContext.Current.CancellationToken);
        return (channel, transport);
    }

    private static ConsumerContext CreateConsumer(
        IServiceProvider services, ConnectionContext connectionContext, string queue, PipelineStep<ConsumeContext> terminal
    )
    {
        var consumerContext = new ConsumerContext(new ChannelContext(connectionContext), queue);
        consumerContext.MessagePipeline = new PipelineBuilder<ConsumeContext>().Build(services, terminal);
        return consumerContext;
    }

    [Fact]
    public async Task Should_publish_and_consume_through_the_pipeline()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var (channel, _) = await OpenChannelAsync(services);
        var topology = channel.Topology!;

        await topology.DeclareExchangeAsync(new ExchangeDefinition("orders"), TestContext.Current.CancellationToken);
        var queue = await topology.DeclareQueueAsync(new QueueDefinition("orders.billing"), TestContext.Current.CancellationToken);
        await topology.BindAsync(new BindingDefinition("orders", queue, "order.*"), TestContext.Current.CancellationToken);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connectionContext = new ConnectionContext("consumer", services);
        var consumerContext = CreateConsumer(services, connectionContext, queue, context =>
        {
            received.TrySetResult(Encoding.UTF8.GetString(context.Body.Span));
            return default;
        });

        await using var consumer = await channel.StartConsumerAsync([consumerContext], TestContext.Current.CancellationToken);

        var publishContext = new PublishContext(new ChannelContext(connectionContext))
        {
            Exchange = "orders",
            RoutingKey = "order.created",
            Body = Encoding.UTF8.GetBytes("hello"),
            CancellationToken = TestContext.Current.CancellationToken
        };
        await channel.PublishAsync(publishContext);

        (await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Should().Be("hello");
    }

    [Theory]
    [InlineData("order.*", "order.created", true)]
    [InlineData("order.*", "order.created.eu", false)]
    [InlineData("order.#", "order.created.eu", true)]
    [InlineData("#", "anything.at.all", true)]
    [InlineData("*.eu", "order.eu", true)]
    [InlineData("order.#.eu", "order.a.b.eu", true)]
    [InlineData("order.#.eu", "order.eu", true)]
    [InlineData("order.*", "payment.created", false)]
    public void Should_match_topic_patterns(string pattern, string routingKey, bool expected)
        => TopicMatcher.Matches(pattern, routingKey).Should().Be(expected);

    [Fact]
    public async Task Should_redeliver_on_nack_requeue()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var (channel, _) = await OpenChannelAsync(services);
        var topology = channel.Topology!;
        var queue = await topology.DeclareQueueAsync(new QueueDefinition("retry.q"), TestContext.Current.CancellationToken);

        var attempts = 0;
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connectionContext = new ConnectionContext("consumer", services);
        var consumerContext = CreateConsumer(services, connectionContext, queue, context =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                context.Ack = AckDecision.NackRequeue;
            }
            else
            {
                context.ReceivedInfo.Redelivered.Should().BeTrue();
                done.TrySetResult(true);
            }
            return default;
        });
        await using var consumer = await channel.StartConsumerAsync([consumerContext], TestContext.Current.CancellationToken);

        // default exchange routes straight to the queue
        await channel.PublishAsync(new PublishContext(new ChannelContext(connectionContext))
        {
            Exchange = "",
            RoutingKey = queue,
            Body = new byte[] { 1 },
            CancellationToken = TestContext.Current.CancellationToken
        });

        (await done.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Should().BeTrue();
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task Should_report_queue_stats_and_purge()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var (channel, transport) = await OpenChannelAsync(services);
        var topology = channel.Topology!;
        var queue = await topology.DeclareQueueAsync(new QueueDefinition("stats.q"), TestContext.Current.CancellationToken);
        var connectionContext = new ConnectionContext("producer", services);

        for (var i = 0; i < 3; i++)
            await channel.PublishAsync(new PublishContext(new ChannelContext(connectionContext))
            {
                Exchange = "",
                RoutingKey = queue,
                Body = new byte[] { (byte)i },
                CancellationToken = TestContext.Current.CancellationToken
            });

        var stats = await topology.GetQueueStatsAsync(queue, TestContext.Current.CancellationToken);
        stats.MessagesCount.Should().Be(3);
        transport.Broker.MessageCount(queue).Should().Be(3);

        await topology.PurgeQueueAsync(queue, TestContext.Current.CancellationToken);
        (await topology.GetQueueStatsAsync(queue, TestContext.Current.CancellationToken)).MessagesCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_declare_server_named_queues()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var (channel, _) = await OpenChannelAsync(services);
        var name = await channel.Topology!.DeclareQueueAsync(new QueueDefinition(), TestContext.Current.CancellationToken);
        name.Should().StartWith("inmemory.gen-");
    }
}
