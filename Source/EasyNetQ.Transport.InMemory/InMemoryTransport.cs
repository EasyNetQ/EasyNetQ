using EasyNetQ.Pipeline;

namespace EasyNetQ.Transport.InMemory;

/// <summary>
///     In-process <see cref="ITransport" /> over an <see cref="InMemoryBroker" />. For tests and benchmarks:
///     no network, no serialization requirements beyond what the pipeline does.
/// </summary>
public sealed class InMemoryTransport : ITransport
{
    /// <summary>
    ///     Creates a transport over a new broker
    /// </summary>
    public InMemoryTransport() : this(new InMemoryBroker())
    {
    }

    /// <summary>
    ///     Creates a transport over an existing broker (share one broker between "processes")
    /// </summary>
    public InMemoryTransport(InMemoryBroker broker) => Broker = broker;

    /// <summary>
    ///     The broker, for assertions
    /// </summary>
    public InMemoryBroker Broker { get; }

    /// <inheritdoc />
    public ValueTask<ITransportConnection> ConnectAsync(ConnectionContext context, CancellationToken cancellationToken = default)
        => new(new InMemoryConnection(Broker));
}

internal sealed class InMemoryConnection(InMemoryBroker broker) : ITransportConnection
{
    public bool IsConnected => true;

    public Task EnsureConnectedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask<ITransportChannel> OpenChannelAsync(ChannelContext context, CancellationToken cancellationToken = default)
        => new(new InMemoryChannel(broker));

    public ValueTask DisposeAsync() => default;
}

internal sealed class InMemoryChannel(InMemoryBroker broker) : ITransportChannel
{
    public ITopology Topology { get; } = new InMemoryTopology(broker);

    public ValueTask PublishAsync(PublishContext context)
    {
        broker.Publish(context.Exchange, context.RoutingKey, context.Properties, context.Body);
        return default;
    }

    public ValueTask<ITransportConsumer> StartConsumerAsync(
        IReadOnlyCollection<ConsumerContext> consumers, CancellationToken cancellationToken = default
    )
    {
        var consumer = new InMemoryConsumer(broker, consumers);
        return new ValueTask<ITransportConsumer>(consumer);
    }

    public ValueTask DisposeAsync() => default;
}

internal sealed class InMemoryConsumer : ITransportConsumer
{
    private readonly CancellationTokenSource cts = new();
    private readonly Task[] pumps;

    public InMemoryConsumer(InMemoryBroker broker, IReadOnlyCollection<ConsumerContext> consumers)
    {
        pumps = consumers.Select(consumerContext => Task.Run(() => PumpAsync(broker, consumerContext, cts.Token))).ToArray();
    }

    private static async Task PumpAsync(InMemoryBroker broker, ConsumerContext consumerContext, CancellationToken cancellationToken)
    {
        var queue = broker.GetQueue(consumerContext.Queue);
        if (queue is null) return;

        Interlocked.Increment(ref queue.ConsumerCount);
        var pipeline = consumerContext.MessagePipeline;
        ulong deliveryTag = 0;
        try
        {
            await foreach (var delivery in queue.Deliveries.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var context = consumerContext.RentMessageContext();
                try
                {
                    context.ReceivedInfo = new MessageReceivedInfo(
                        "inmemory", ++deliveryTag, delivery.Redelivered, delivery.Exchange, delivery.RoutingKey, consumerContext.Queue
                    );
                    context.Properties = delivery.Properties;
                    context.Body = delivery.Body;
                    context.CancellationToken = cancellationToken;

                    await pipeline!(context).ConfigureAwait(false);

                    if (!consumerContext.AutoAck && context.Ack == AckDecision.NackRequeue)
                        broker.Redeliver(consumerContext.Queue, delivery);
                }
                finally
                {
                    consumerContext.ReturnMessageContext(context);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            Interlocked.Decrement(ref queue.ConsumerCount);
        }
    }

    public async ValueTask DisposeAsync()
    {
        cts.Cancel();
        try
        {
            await Task.WhenAll(pumps).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        cts.Dispose();
    }
}

internal sealed class InMemoryTopology(InMemoryBroker broker) : ITopology
{
    public ValueTask DeclareExchangeAsync(ExchangeDefinition exchange, CancellationToken cancellationToken = default)
    {
        broker.DeclareExchange(exchange);
        return default;
    }

    public ValueTask DeclareExchangePassiveAsync(string exchange, CancellationToken cancellationToken = default)
        => broker.ExchangeExists(exchange) ? default : throw new EasyNetQException($"Exchange {exchange} does not exist");

    public ValueTask DeleteExchangeAsync(string exchange, bool ifUnused = false, CancellationToken cancellationToken = default)
    {
        broker.DeleteExchange(exchange);
        return default;
    }

    public ValueTask<string> DeclareQueueAsync(QueueDefinition queue, CancellationToken cancellationToken = default)
        => new(broker.DeclareQueue(queue));

    public ValueTask DeclareQueuePassiveAsync(string queue, CancellationToken cancellationToken = default)
        => broker.QueueExists(queue) ? default : throw new EasyNetQException($"Queue {queue} does not exist");

    public ValueTask DeleteQueueAsync(string queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default)
    {
        broker.DeleteQueue(queue);
        return default;
    }

    public ValueTask PurgeQueueAsync(string queue, CancellationToken cancellationToken = default)
    {
        broker.Purge(queue);
        return default;
    }

    public ValueTask BindAsync(BindingDefinition binding, CancellationToken cancellationToken = default)
    {
        broker.Bind(binding);
        return default;
    }

    public ValueTask UnbindAsync(BindingDefinition binding, CancellationToken cancellationToken = default)
    {
        broker.Unbind(binding);
        return default;
    }

    public ValueTask<QueueStats> GetQueueStatsAsync(string queue, CancellationToken cancellationToken = default)
    {
        var q = broker.GetQueue(queue);
        return new ValueTask<QueueStats>(new QueueStats(
            (ulong)(q?.Deliveries.Reader.Count ?? 0),
            (ulong)(q?.ConsumerCount ?? 0)
        ));
    }
}
