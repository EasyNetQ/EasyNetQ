using System.Text;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Core.Tests;

public class InMemoryTtlTests
{
    private static async Task<(ITransportChannel Channel, InMemoryBroker Broker, ConnectionContext Context)> OpenChannelAsync(IServiceProvider services)
    {
        var transport = new InMemoryTransport();
        var connectionContext = new ConnectionContext("test", services);
        var connection = await transport.ConnectAsync(connectionContext, TestContext.Current.CancellationToken);
        return (await connection.OpenChannelAsync(new ChannelContext(connectionContext), TestContext.Current.CancellationToken), transport.Broker, connectionContext);
    }

    private static ValueTask PublishAsync(ITransportChannel channel, ConnectionContext connectionContext, string exchange, string routingKey, TimeSpan? expiration = null)
        => channel.PublishAsync(new PublishContext(new ChannelContext(connectionContext))
        {
            Exchange = exchange,
            RoutingKey = routingKey,
            Properties = new MessageProperties { Expiration = expiration },
            Body = Encoding.UTF8.GetBytes("later"),
            CancellationToken = TestContext.Current.CancellationToken
        });

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }

    [Fact]
    public async Task Should_dead_letter_expired_messages_like_the_DLX_scheduler_expects()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var (channel, broker, connectionContext) = await OpenChannelAsync(services);
        var topology = channel.Topology!;
        await topology.DeclareExchangeAsync(new ExchangeDefinition("reminders"), TestContext.Current.CancellationToken);
        await topology.DeclareExchangeAsync(new ExchangeDefinition("reminders_00_00_00_01"), TestContext.Current.CancellationToken);
        var delayQueue = await topology.DeclareQueueAsync(new QueueDefinition("reminders_00_00_00_01")
        {
            Arguments = new Dictionary<string, object> { [Argument.MessageTtl] = 100, [Argument.DeadLetterExchange] = "reminders" }
        }, TestContext.Current.CancellationToken);
        await topology.BindAsync(new BindingDefinition("reminders_00_00_00_01", delayQueue, "#"), TestContext.Current.CancellationToken);
        var target = await topology.DeclareQueueAsync(new QueueDefinition("reminders.app"), TestContext.Current.CancellationToken);
        await topology.BindAsync(new BindingDefinition("reminders", target, "reminder.*"), TestContext.Current.CancellationToken);

        await PublishAsync(channel, connectionContext, "reminders_00_00_00_01", "reminder.due");
        broker.MessageCount(delayQueue).Should().Be(1);
        broker.MessageCount(target).Should().Be(0);

        await WaitUntil(() => broker.MessageCount(target) == 1);
        broker.MessageCount(delayQueue).Should().Be(0, "the expired message left the delay queue");
    }

    [Fact]
    public async Task Should_drop_an_expired_message_without_dead_letter_exchange_and_never_deliver_it()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var (channel, broker, connectionContext) = await OpenChannelAsync(services);
        var queue = await channel.Topology!.DeclareQueueAsync(new QueueDefinition("requests"), TestContext.Current.CancellationToken);

        await PublishAsync(channel, connectionContext, "", queue, expiration: TimeSpan.FromMilliseconds(20));
        await WaitUntil(() => broker.MessageCount(queue) == 0);

        var delivered = 0;
        var consumerContext = new ConsumerContext(new ChannelContext(connectionContext), queue)
        {
            MessagePipeline = new PipelineBuilder<ConsumeContext>().Build(services, _ =>
            {
                Interlocked.Increment(ref delivered);
                return default;
            })
        };
        await using (await channel.StartConsumerAsync([consumerContext], TestContext.Current.CancellationToken))
        {
            await PublishAsync(channel, connectionContext, "", queue, expiration: TimeSpan.FromMinutes(1));
            await WaitUntil(() => delivered == 1);
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        delivered.Should().Be(1, "only the message that had not expired is delivered");
    }
}
