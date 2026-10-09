using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;

namespace EasyNetQ.Core.Tests;

/// <summary>
///     InMemory transport with RabbitMQ's topology rules: binding to an exchange nobody declared fails (the broker's
///     404), and every exchange declaration is recorded. <see cref="FailConnects" /> makes connecting fail, like an
///     unreachable broker. <see cref="OpenConnections" /> counts connections not disposed yet.
/// </summary>
public sealed class StrictTopologyTransport : ITransport
{
    private readonly InMemoryTransport inner = new();

    public InMemoryTransport Inner => inner;
    public List<string> DeclaredExchanges { get; } = new();
    public int FailConnects { get; set; }
    public int OpenConnections => openConnections;
    private int openConnections;

    public void DeclareExternally(string exchange) => inner.Broker.DeclareExchange(new ExchangeDefinition(exchange));

    public async ValueTask<ITransportConnection> ConnectAsync(ConnectionContext context, CancellationToken cancellationToken = default)
    {
        if (FailConnects > 0)
        {
            FailConnects--;
            throw new EasyNetQException("broker unreachable");
        }
        var connection = new Connection(await inner.ConnectAsync(context, cancellationToken), this);
        Interlocked.Increment(ref openConnections);
        return connection;
    }

    private sealed class Connection(ITransportConnection connection, StrictTopologyTransport transport) : ITransportConnection
    {
        public bool IsConnected => connection.IsConnected;
        public Task EnsureConnectedAsync(CancellationToken cancellationToken = default) => connection.EnsureConnectedAsync(cancellationToken);

        public async ValueTask<ITransportChannel> OpenChannelAsync(ChannelContext context, CancellationToken cancellationToken = default)
            => new Channel(await connection.OpenChannelAsync(context, cancellationToken), transport);

        public ValueTask DisposeAsync()
        {
            Interlocked.Decrement(ref transport.openConnections);
            return connection.DisposeAsync();
        }
    }

    private sealed class Channel(ITransportChannel channel, StrictTopologyTransport transport) : ITransportChannel
    {
        public ITopology? Topology { get; } = new Topology(channel.Topology!, transport);
        public ValueTask PublishAsync(PublishContext context) => channel.PublishAsync(context);

        public ValueTask<ITransportConsumer> StartConsumerAsync(IReadOnlyCollection<ConsumerContext> consumers, CancellationToken cancellationToken = default)
            => channel.StartConsumerAsync(consumers, cancellationToken);

        public ValueTask DisposeAsync() => channel.DisposeAsync();
    }

    private sealed class Topology(ITopology topology, StrictTopologyTransport transport) : ITopology
    {
        public ValueTask DeclareExchangeAsync(ExchangeDefinition exchange, CancellationToken cancellationToken = default)
        {
            lock (transport.DeclaredExchanges) transport.DeclaredExchanges.Add(exchange.Name);
            return topology.DeclareExchangeAsync(exchange, cancellationToken);
        }

        public ValueTask BindAsync(BindingDefinition binding, CancellationToken cancellationToken = default)
            => transport.inner.Broker.Exchanges.ContainsKey(binding.Source)
                ? topology.BindAsync(binding, cancellationToken)
                : throw new EasyNetQException($"NOT_FOUND - no exchange '{binding.Source}'");

        public ValueTask DeclareExchangePassiveAsync(string exchange, CancellationToken cancellationToken = default) => topology.DeclareExchangePassiveAsync(exchange, cancellationToken);
        public ValueTask DeleteExchangeAsync(string exchange, bool ifUnused = false, CancellationToken cancellationToken = default) => topology.DeleteExchangeAsync(exchange, ifUnused, cancellationToken);
        public ValueTask<string> DeclareQueueAsync(QueueDefinition queue, CancellationToken cancellationToken = default) => topology.DeclareQueueAsync(queue, cancellationToken);
        public ValueTask DeclareQueuePassiveAsync(string queue, CancellationToken cancellationToken = default) => topology.DeclareQueuePassiveAsync(queue, cancellationToken);
        public ValueTask DeleteQueueAsync(string queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default) => topology.DeleteQueueAsync(queue, ifUnused, ifEmpty, cancellationToken);
        public ValueTask PurgeQueueAsync(string queue, CancellationToken cancellationToken = default) => topology.PurgeQueueAsync(queue, cancellationToken);
        public ValueTask UnbindAsync(BindingDefinition binding, CancellationToken cancellationToken = default) => topology.UnbindAsync(binding, cancellationToken);
        public ValueTask<QueueStats> GetQueueStatsAsync(string queue, CancellationToken cancellationToken = default) => topology.GetQueueStatsAsync(queue, cancellationToken);
    }
}
