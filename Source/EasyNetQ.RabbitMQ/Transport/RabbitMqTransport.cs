using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Events;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Transport;

/// <summary>
///     RabbitMQ implementation of <see cref="ITransport" />. Logical connections map onto the persistent
///     producer/consumer connections (per <see cref="Keys.ConnectionType" />); logical channels map onto the
///     persistent channel dispatcher, which pools the physical channels.
/// </summary>
public sealed class RabbitMqTransport : ITransport
{
    private readonly IPersistentChannelDispatcher persistentChannelDispatcher;
    private readonly IProducerConnection producerConnection;
    private readonly IConsumerConnection consumerConnection;
    private readonly IConsumerFactory consumerFactory;

    /// <summary>
    ///     Creates the transport
    /// </summary>
    public RabbitMqTransport(
        IPersistentChannelDispatcher persistentChannelDispatcher,
        IProducerConnection producerConnection,
        IConsumerConnection consumerConnection,
        IConsumerFactory consumerFactory
    )
    {
        this.persistentChannelDispatcher = persistentChannelDispatcher;
        this.producerConnection = producerConnection;
        this.consumerConnection = consumerConnection;
        this.consumerFactory = consumerFactory;
    }

    /// <inheritdoc />
    public ValueTask<ITransportConnection> ConnectAsync(ConnectionContext context, CancellationToken cancellationToken = default)
    {
        context.TryGet(Keys.ConnectionType, out var type);
        IPersistentConnection connection = type == PersistentConnectionType.Consumer ? consumerConnection : producerConnection;
        var notifier = context.Services.GetService<LifecycleNotifier>();
        var eventBus = context.Services.GetService<IEventBus>();
        return new ValueTask<ITransportConnection>(
            new RabbitMqTransportConnection(connection, type, persistentChannelDispatcher, consumerFactory, context, notifier, eventBus)
        );
    }
}

internal sealed class RabbitMqTransportConnection : ITransportConnection
{
    private readonly IPersistentConnection connection;
    private readonly PersistentConnectionType type;
    private readonly IPersistentChannelDispatcher persistentChannelDispatcher;
    private readonly IConsumerFactory consumerFactory;
    private readonly LifecycleNotifier? notifier;
    private readonly IDisposable[] lifecycleSubscriptions;

    public RabbitMqTransportConnection(
        IPersistentConnection connection,
        PersistentConnectionType type,
        IPersistentChannelDispatcher persistentChannelDispatcher,
        IConsumerFactory consumerFactory,
        ConnectionContext context,
        LifecycleNotifier? notifier,
        IEventBus? eventBus
    )
    {
        this.connection = connection;
        this.type = type;
        this.persistentChannelDispatcher = persistentChannelDispatcher;
        this.consumerFactory = consumerFactory;
        this.notifier = notifier;

        // bridge the internal events onto the lifecycle pipeline; the internal bus goes away in phase 6
        lifecycleSubscriptions = notifier is { IsEnabled: true } && eventBus is not null
            ?
            [
                eventBus.Subscribe<ConnectionCreatedEvent>(e => NotifyAsync(e.Type, context, LifecycleEvent.Connected)),
                eventBus.Subscribe<ConnectionRecoveredEvent>(e => NotifyAsync(e.Type, context, LifecycleEvent.Recovered)),
                eventBus.Subscribe<ConnectionDisconnectedEvent>(e => NotifyAsync(e.Type, context, LifecycleEvent.Disconnected, e.Reason)),
                eventBus.Subscribe<ConnectionBlockedEvent>(e => NotifyAsync(e.Type, context, LifecycleEvent.Blocked, e.Reason)),
                eventBus.Subscribe<ConnectionUnblockedEvent>(e => NotifyAsync(e.Type, context, LifecycleEvent.Unblocked)),
                eventBus.Subscribe<ConnectionRecoveryErrorEvent>(e => NotifyAsync(e.Type, context, LifecycleEvent.RecoveryError, error: e.Exception)),
                eventBus.Subscribe<ConnectionCallbackErrorEvent>(e => NotifyAsync(e.Type, context, LifecycleEvent.CallbackError, error: e.Exception)),
            ]
            : [];
    }

    private Task NotifyAsync(
        PersistentConnectionType eventType, ConnectionContext context, LifecycleEvent @event,
        string? reason = null, Exception? error = null
    )
        => eventType == type
            ? notifier!.NotifyAsync(context, LifecycleLayer.Connection, @event, reason, error).AsTask()
            : Task.CompletedTask;

    public bool IsConnected => connection.Status.State == PersistentConnectionState.Connected;

    public Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
        => connection.ConnectAsync();

    public ValueTask<ITransportChannel> OpenChannelAsync(ChannelContext context, CancellationToken cancellationToken = default)
        => new(new RabbitMqTransportChannel(type, persistentChannelDispatcher, consumerFactory));

    // the persistent connections are owned by the container; only the lifecycle bridge is ours
    public ValueTask DisposeAsync()
    {
        foreach (var subscription in lifecycleSubscriptions)
            subscription.Dispose();
        return default;
    }
}

internal sealed class RabbitMqTransportChannel : ITransportChannel
{
    private readonly PersistentConnectionType type;
    private readonly IPersistentChannelDispatcher persistentChannelDispatcher;
    private readonly IConsumerFactory consumerFactory;
    private readonly PersistentChannelDispatchOptions publishOptions;
    private readonly PersistentChannelDispatchOptions publishWithConfirmsOptions;

    public RabbitMqTransportChannel(
        PersistentConnectionType type,
        IPersistentChannelDispatcher persistentChannelDispatcher,
        IConsumerFactory consumerFactory
    )
    {
        this.type = type;
        this.persistentChannelDispatcher = persistentChannelDispatcher;
        this.consumerFactory = consumerFactory;
        publishOptions = new PersistentChannelDispatchOptions("Publish", type);
        publishWithConfirmsOptions = new PersistentChannelDispatchOptions("PublishWithConfirms", type, PublisherConfirms: true);
        Topology = new RabbitMqTopology(persistentChannelDispatcher, new PersistentChannelDispatchOptions("Topology", type));
    }

    public ITopology Topology { get; }

    ITopology? ITransportChannel.Topology => Topology;

    public async ValueTask PublishAsync(PublishContext context)
    {
        if (context.PublisherConfirms)
        {
            // The action starts the publish inside the channel mutex and hands back the in-flight task; the
            // client-side confirmation tracking completes it when the broker confirms. Awaiting it here, outside
            // the mutex, keeps confirmed publishes concurrent (bounded by the channel's rate limiter).
            var publishTask = await persistentChannelDispatcher.InvokeAsync<Task, StartConfirmedPublishAction>(
                new StartConfirmedPublishAction(context.Exchange, context.RoutingKey, context.Mandatory, context.Properties, context.Body),
                publishWithConfirmsOptions,
                context.CancellationToken
            ).ConfigureAwait(false);

            try
            {
                await publishTask.ConfigureAwait(false);
            }
            catch (PublishReturnException exception)
            {
                throw new PublishReturnedException(
                    $"Broker has returned the message: {exception.ReplyCode} {exception.ReplyText} (exchange={exception.Exchange}, routingKey={exception.RoutingKey})",
                    exception
                );
            }
            catch (PublishException exception)
            {
                throw new PublishNackedException(
                    $"Broker has signalled that the publish {exception.PublishSequenceNumber} was nacked", exception
                );
            }
        }
        else
        {
            await persistentChannelDispatcher.InvokeAsync<bool, BasicPublishAction>(
                new BasicPublishAction(context.Exchange, context.RoutingKey, context.Mandatory, context.Properties, context.Body),
                publishOptions,
                context.CancellationToken
            ).ConfigureAwait(false);
        }
    }

    public async ValueTask<ITransportConsumer> StartConsumerAsync(
        IReadOnlyCollection<ConsumerContext> consumers, CancellationToken cancellationToken = default
    )
    {
        var perQueueConfigurations = new Dictionary<Topology.Queue, PerQueueConsumerConfiguration>();
        ushort prefetchCount = 0;
        foreach (var consumerContext in consumers)
        {
            if (!consumerContext.TryGet(RabbitKeys.Queue, out var queue))
                queue = new Topology.Queue(consumerContext.Queue, isDurable: true);
            consumerContext.TryGet(RabbitKeys.ConsumerTag, out var consumerTag);
            consumerContext.TryGet(RabbitKeys.ExclusiveConsumer, out var exclusive);
            consumerContext.TryGet(RabbitKeys.ConsumerArguments, out var arguments);
            prefetchCount = Math.Max(prefetchCount, consumerContext.PrefetchCount);
            perQueueConfigurations.Add(
                queue,
                new PerQueueConsumerConfiguration(consumerContext.AutoAck, consumerTag ?? "", exclusive, arguments, consumerContext)
            );
        }

        var consumer = consumerFactory.CreateConsumer(new ConsumerConfiguration(prefetchCount, perQueueConfigurations));
        await consumer.StartConsumingAsync(cancellationToken).ConfigureAwait(false);

        var notifier = consumers.Count > 0 ? consumers.First().Services.GetService<LifecycleNotifier>() : null;
        if (notifier is { IsEnabled: true })
            foreach (var consumerContext in consumers)
                await notifier.NotifyAsync(consumerContext, LifecycleLayer.Consumer, LifecycleEvent.Started, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new RabbitMqTransportConsumer(consumer, consumers, notifier);
    }

    public ValueTask DisposeAsync() => default;

    private readonly struct StartConfirmedPublishAction : IPersistentChannelAction<Task>
    {
        private readonly string exchange;
        private readonly string routingKey;
        private readonly bool mandatory;
        private readonly MessageProperties properties;
        private readonly ReadOnlyMemory<byte> body;

        public StartConfirmedPublishAction(
            string exchange, string routingKey, bool mandatory, in MessageProperties properties, in ReadOnlyMemory<byte> body
        )
        {
            this.exchange = exchange;
            this.routingKey = routingKey;
            this.mandatory = mandatory;
            this.properties = properties;
            this.body = body;
        }

        public Task<Task> InvokeAsync(IChannel channel, CancellationToken cancellationToken = default)
        {
            // BasicPublishAsync surfaces a dead channel only through the returned task, which is awaited outside
            // the mutex where the recreate-on-failure verdicts cannot see it - so detect it here, inside the
            // mutex, and have the channel recreated and the publish retried on a fresh one
            if (channel.CloseReason is { } closeReason)
                throw new StaleChannelException(closeReason);

            var basicProperties = new BasicProperties();
            properties.CopyTo(basicProperties);

            return Task.FromResult(channel.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, cancellationToken).AsTask());
        }
    }

    private readonly struct BasicPublishAction : IPersistentChannelAction<bool>
    {
        private readonly string exchange;
        private readonly string routingKey;
        private readonly bool mandatory;
        private readonly MessageProperties properties;
        private readonly ReadOnlyMemory<byte> body;

        public BasicPublishAction(
            string exchange, string routingKey, bool mandatory, in MessageProperties properties, in ReadOnlyMemory<byte> body
        )
        {
            this.exchange = exchange;
            this.routingKey = routingKey;
            this.mandatory = mandatory;
            this.properties = properties;
            this.body = body;
        }

        public async Task<bool> InvokeAsync(IChannel channel, CancellationToken cancellationToken = default)
        {
            var basicProperties = new BasicProperties();
            properties.CopyTo(basicProperties);

            await channel.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, cancellationToken);
            return true;
        }
    }
}

internal sealed class RabbitMqTransportConsumer : ITransportConsumer
{
    private readonly IConsumer consumer;
    private readonly IReadOnlyCollection<ConsumerContext> consumers;
    private readonly LifecycleNotifier? notifier;

    public RabbitMqTransportConsumer(IConsumer consumer, IReadOnlyCollection<ConsumerContext> consumers, LifecycleNotifier? notifier)
    {
        this.consumer = consumer;
        this.consumers = consumers;
        this.notifier = notifier;
    }

    public async ValueTask DisposeAsync()
    {
        await consumer.DisposeAsync().ConfigureAwait(false);
        if (notifier is { IsEnabled: true })
            foreach (var consumerContext in consumers)
                await notifier.NotifyAsync(consumerContext, LifecycleLayer.Consumer, LifecycleEvent.Stopped).ConfigureAwait(false);
    }
}
