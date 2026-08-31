using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Publish -> consume through the transport abstraction and the pipeline, no broker: the framework
///     plumbing alone. Target: 0 B per round trip beyond what the harness itself allocates.
/// </summary>
[MemoryDiagnoser]
public class EndToEndInMemoryBenchmarks
{
    private ServiceProvider provider = null!;
    private ITransportChannel channel = null!;
    private ITransportConsumer consumer = null!;
    private ContextPool<PublishContext> publishContextPool = null!;
    private readonly Channel<byte> completions = System.Threading.Channels.Channel.CreateUnbounded<byte>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true }
    );
    private byte[] body = null!;
    private const string Queue = "bench.q";

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        provider = new ServiceCollection().BuildServiceProvider();
        var transport = new InMemoryTransport();
        var connectionContext = new ConnectionContext("bench", provider);
        var connection = await transport.ConnectAsync(connectionContext);
        channel = await connection.OpenChannelAsync(new ChannelContext(connectionContext));
        await channel.Topology!.DeclareQueueAsync(new QueueDefinition(Queue));

        var completionWriter = completions.Writer;
        var consumerContext = new ConsumerContext(new ChannelContext(connectionContext), Queue);
        consumerContext.MessagePipeline = new PipelineBuilder<ConsumeContext>().Build(
            provider,
            context =>
            {
                completionWriter.TryWrite(0);
                return default;
            }
        );
        consumer = await channel.StartConsumerAsync([consumerContext]);

        var publishChannelContext = new ChannelContext(connectionContext);
        publishContextPool = new ContextPool<PublishContext>(() => new PublishContext(publishChannelContext));
        body = new byte[64];
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await consumer.DisposeAsync();
        await provider.DisposeAsync();
    }

    [Benchmark]
    public async Task PublishAndConsume()
    {
        var context = publishContextPool.Rent();
        try
        {
            context.Exchange = "";
            context.RoutingKey = Queue;
            context.Body = body;
            await channel.PublishAsync(context);
        }
        finally
        {
            publishContextPool.Return(context);
        }

        await completions.Reader.ReadAsync();
    }
}
