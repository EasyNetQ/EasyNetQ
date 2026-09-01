using EasyNetQ.ChannelDispatcher;

namespace EasyNetQ.Transport;

/// <summary>
///     Topology operations over the persistent channel dispatcher
/// </summary>
internal sealed class RabbitMqTopology : ITopology
{
    private readonly IPersistentChannelDispatcher persistentChannelDispatcher;
    private readonly PersistentChannelDispatchOptions dispatchOptions;

    public RabbitMqTopology(IPersistentChannelDispatcher persistentChannelDispatcher, in PersistentChannelDispatchOptions dispatchOptions)
    {
        this.persistentChannelDispatcher = persistentChannelDispatcher;
        this.dispatchOptions = dispatchOptions;
    }

    public async ValueTask DeclareExchangeAsync(ExchangeDefinition exchange, CancellationToken cancellationToken = default)
    {
        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.ExchangeDeclareAsync(exchange.Name, exchange.Type, exchange.Durable, exchange.AutoDelete, exchange.Arguments, cancellationToken: cancellationToken),
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async ValueTask DeclareExchangePassiveAsync(string exchange, CancellationToken cancellationToken = default)
    {
        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.ExchangeDeclarePassiveAsync(exchange, cancellationToken),
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async ValueTask DeleteExchangeAsync(string exchange, bool ifUnused = false, CancellationToken cancellationToken = default)
    {
        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.ExchangeDeleteAsync(exchange, ifUnused, cancellationToken: cancellationToken),
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async ValueTask<string> DeclareQueueAsync(QueueDefinition queue, CancellationToken cancellationToken = default)
    {
        var declareResult = await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueDeclareAsync(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments, cancellationToken: cancellationToken),
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
        return declareResult.QueueName;
    }

    public async ValueTask DeclareQueuePassiveAsync(string queue, CancellationToken cancellationToken = default)
    {
        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueDeclarePassiveAsync(queue, cancellationToken),
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async ValueTask DeleteQueueAsync(string queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default)
    {
        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueDeleteAsync(queue, ifUnused, ifEmpty, cancellationToken: cancellationToken),
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async ValueTask PurgeQueueAsync(string queue, CancellationToken cancellationToken = default)
    {
        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueuePurgeAsync(queue, cancellationToken),
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async ValueTask BindAsync(BindingDefinition binding, CancellationToken cancellationToken = default)
    {
        await persistentChannelDispatcher.InvokeAsync(
            async x =>
            {
                if (binding.DestinationIsExchange)
                    await x.ExchangeBindAsync(binding.Destination, binding.Source, binding.RoutingKey, binding.Arguments, cancellationToken: cancellationToken);
                else
                    await x.QueueBindAsync(binding.Destination, binding.Source, binding.RoutingKey, binding.Arguments, cancellationToken: cancellationToken);
                return true;
            },
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async ValueTask UnbindAsync(BindingDefinition binding, CancellationToken cancellationToken = default)
    {
        await persistentChannelDispatcher.InvokeAsync(
            async x =>
            {
                if (binding.DestinationIsExchange)
                    await x.ExchangeUnbindAsync(binding.Destination, binding.Source, binding.RoutingKey, binding.Arguments, cancellationToken: cancellationToken);
                else
                    await x.QueueUnbindAsync(binding.Destination, binding.Source, binding.RoutingKey, binding.Arguments, cancellationToken);
                return true;
            },
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async ValueTask<QueueStats> GetQueueStatsAsync(string queue, CancellationToken cancellationToken = default)
    {
        var declareResult = await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueDeclarePassiveAsync(queue, cancellationToken),
            dispatchOptions,
            cancellationToken
        ).ConfigureAwait(false);
        return new QueueStats(declareResult.MessageCount, declareResult.ConsumerCount);
    }
}
