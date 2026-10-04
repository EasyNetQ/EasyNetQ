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

public class When_the_in_memory_broker_behaves_like_amqp
{
    private static async Task<(ITransportChannel Channel, ITopology Topology, ConnectionContext Connection, ServiceProvider Services, List<LifecycleContext> Events)> OpenAsync()
    {
        var events = new List<LifecycleContext>();
        var services = new ServiceCollection()
            .AddSingleton(new PipelineBuilder<LifecycleContext>())
            .AddSingleton(new LifecycleConfiguration(b => b.Use("record", (context, next) =>
            {
                lock (events) events.Add(context);
                return next(context);
            })))
            .AddSingleton<LifecycleNotifier>()
            .BuildServiceProvider();
        var connectionContext = new ConnectionContext("test", services);
        var connection = await new InMemoryTransport().ConnectAsync(connectionContext, TestContext.Current.CancellationToken);
        var channel = await connection.OpenChannelAsync(new ChannelContext(connectionContext), TestContext.Current.CancellationToken);
        return (channel, channel.Topology!, connectionContext, services, events);
    }

    private static PublishContext Publish(ConnectionContext connection, string exchange, string key, bool mandatory = false) => new(new ChannelContext(connection))
    {
        Exchange = exchange,
        RoutingKey = key,
        Mandatory = mandatory,
        Body = Encoding.UTF8.GetBytes(key),
        CancellationToken = TestContext.Current.CancellationToken,
    };

    [Fact]
    public async Task Should_return_an_unroutable_mandatory_publish()
    {
        var (channel, topology, connection, services, _) = await OpenAsync();
        await using var _ = services;
        await topology.DeclareExchangeAsync(new ExchangeDefinition("x", ExchangeType.Direct), TestContext.Current.CancellationToken);

        var mandatory = async () => await channel.PublishAsync(Publish(connection, "x", "nobody", mandatory: true));
        var fireAndForget = async () => await channel.PublishAsync(Publish(connection, "x", "nobody"));

        await mandatory.Should().ThrowAsync<UnroutableMessageException>();
        await fireAndForget.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_treat_a_repeated_binding_as_one()
    {
        var (channel, topology, connection, services, _) = await OpenAsync();
        await using var _ = services;
        await topology.DeclareExchangeAsync(new ExchangeDefinition("x", ExchangeType.Direct), TestContext.Current.CancellationToken);
        var queue = await topology.DeclareQueueAsync(new QueueDefinition("q"), TestContext.Current.CancellationToken);
        await topology.BindAsync(new BindingDefinition("x", queue, "k"), TestContext.Current.CancellationToken);
        await topology.BindAsync(new BindingDefinition("x", queue, "k"), TestContext.Current.CancellationToken);

        await channel.PublishAsync(Publish(connection, "x", "k"));

        (await topology.GetQueueStatsAsync(queue, TestContext.Current.CancellationToken)).MessagesCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_keep_routing_other_keys_while_unbinding()
    {
        var (channel, topology, connection, services, _) = await OpenAsync();
        await using var _ = services;
        await topology.DeclareExchangeAsync(new ExchangeDefinition("x", ExchangeType.Direct), TestContext.Current.CancellationToken);
        var queue = await topology.DeclareQueueAsync(new QueueDefinition("q"), TestContext.Current.CancellationToken);
        await topology.BindAsync(new BindingDefinition("x", queue, "stays"), TestContext.Current.CancellationToken);

        var churn = Task.Run(async () =>
        {
            for (var i = 0; i < 2_000; i++)
            {
                await topology.BindAsync(new BindingDefinition("x", queue, $"churn.{i}"));
                await topology.UnbindAsync(new BindingDefinition("x", queue, $"churn.{i}"));
            }
        }, TestContext.Current.CancellationToken);
        for (var i = 0; i < 2_000; i++)
            await channel.PublishAsync(Publish(connection, "x", "stays", mandatory: true));
        await churn;

        (await topology.GetQueueStatsAsync(queue, TestContext.Current.CancellationToken)).MessagesCount.Should().Be(2_000);
    }

    [Fact]
    public async Task Should_report_a_deleted_queue_as_a_cancelled_consumer()
    {
        var (channel, topology, connection, services, events) = await OpenAsync();
        await using var _ = services;
        var queue = await topology.DeclareQueueAsync(new QueueDefinition("q"), TestContext.Current.CancellationToken);
        var consumerContext = new ConsumerContext(new ChannelContext(connection), queue) { MessagePipeline = static _ => default };
        await using var consumer = await channel.StartConsumerAsync([consumerContext], TestContext.Current.CancellationToken);

        await topology.DeleteQueueAsync(queue, cancellationToken: TestContext.Current.CancellationToken);

        for (var i = 0; i < 100 && !Cancelled(); i++)
            await Task.Delay(20, TestContext.Current.CancellationToken);
        Cancelled().Should().BeTrue();

        bool Cancelled()
        {
            lock (events) return events.Any(e => e.Layer == LifecycleLayer.Consumer && e.Event == LifecycleEvent.Cancelled);
        }
    }
}
