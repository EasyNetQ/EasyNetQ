using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using EasyNetQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ;

/// <inheritdoc cref="IAdvancedBus"/>
public class RabbitAdvancedBus : IAdvancedBus, IDisposable
{
    private readonly ConnectionConfiguration configuration;
    private readonly PipelineBuilder<ConsumeContext> consumePipelineBuilder;
    private readonly IServiceProvider services;
    private readonly ILogger logger;
    private readonly IProducerConnection producerConnection;
    private readonly IConsumerConnection consumerConnection;
    private readonly IEventBus eventBus;
    private readonly IDisposable[] eventSubscriptions;
    private readonly IHandlerCollectionFactory handlerCollectionFactory;
    private readonly IMessageSerializationStrategy messageSerializationStrategy;
    private readonly SelectSerializerStep selectSerializerStep;
    private static readonly ResolveMessageTypeStep ResolveMessageTypeStep = new();
    private static readonly ResolveHandlerStep ResolveHandlerStep = new();
    private static readonly DeserializeStep DeserializeStep = new();
    private readonly IPullingConsumerFactory pullingConsumerFactory;
    private readonly AdvancedBusEventHandlers advancedBusEventHandlers;

    private readonly ITransportConnection producerTransportConnection;
    private readonly ITransportConnection consumerTransportConnection;
    private readonly ITransportChannel producerChannel;
    private readonly ITransportChannel consumerChannel;
    private readonly ITopology producerTopology;
    private readonly ITopology consumerTopology;

    private volatile bool disposed;
    private readonly PipelineStep<PublishContext> publishPipeline;
    private readonly ContextPool<PublishContext> publishContextPool;
    private readonly ConnectionContext producerConnectionContext;
    private readonly ConnectionContext consumerConnectionContext;

    /// <summary>
    ///     Creates RabbitAdvancedBus
    /// </summary>
    public RabbitAdvancedBus(
        ILogger<RabbitAdvancedBus> logger,
        IProducerConnection producerConnection,
        IConsumerConnection consumerConnection,
        ITransport transport,
        IEventBus eventBus,
        IHandlerCollectionFactory handlerCollectionFactory,
        ConnectionConfiguration configuration,
        PipelineBuilder<PublishContext> publishPipelineBuilder,
        PipelineBuilder<ConsumeContext> consumePipelineBuilder,
        IServiceProvider services,
        IMessageSerializationStrategy messageSerializationStrategy,
        IMessageSerializer messageSerializer,
        IPullingConsumerFactory pullingConsumerFactory,
        AdvancedBusEventHandlers advancedBusEventHandlers
    )
    {
        this.logger = logger;
        this.producerConnection = producerConnection;
        this.consumerConnection = consumerConnection;
        this.eventBus = eventBus;
        this.handlerCollectionFactory = handlerCollectionFactory;
        this.configuration = configuration;
        this.consumePipelineBuilder = consumePipelineBuilder;
        this.services = services;
        this.messageSerializationStrategy = messageSerializationStrategy;
        selectSerializerStep = new SelectSerializerStep(messageSerializer);
        this.pullingConsumerFactory = pullingConsumerFactory;
        this.advancedBusEventHandlers = advancedBusEventHandlers;

        Connected += advancedBusEventHandlers.Connected;
        Disconnected += advancedBusEventHandlers.Disconnected;
        Blocked += advancedBusEventHandlers.Blocked;
        Unblocked += advancedBusEventHandlers.Unblocked;
        MessageReturned += advancedBusEventHandlers.MessageReturned;

        eventSubscriptions =
        [
            this.eventBus.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated),
            this.eventBus.Subscribe<ConnectionRecoveredEvent>(OnConnectionRecovered),
            this.eventBus.Subscribe<ConnectionDisconnectedEvent>(OnConnectionDisconnected),
            this.eventBus.Subscribe<ConnectionBlockedEvent>(OnConnectionBlocked),
            this.eventBus.Subscribe<ConnectionUnblockedEvent>(OnConnectionUnblocked),
            this.eventBus.Subscribe<ReturnedMessageEvent>(OnMessageReturned)
        ];

        producerConnectionContext = new ConnectionContext("Producer", services);
        producerConnectionContext.Set(Keys.ConnectionType, PersistentConnectionType.Producer);
        consumerConnectionContext = new ConnectionContext("Consumer", services);
        consumerConnectionContext.Set(Keys.ConnectionType, PersistentConnectionType.Consumer);

        // the RabbitMQ transport completes these synchronously; physical connections are established lazily
        producerTransportConnection = transport.ConnectAsync(producerConnectionContext).AsTask().GetAwaiter().GetResult();
        consumerTransportConnection = transport.ConnectAsync(consumerConnectionContext).AsTask().GetAwaiter().GetResult();
        var publishChannelContext = new ChannelContext(producerConnectionContext);
        producerChannel = producerTransportConnection.OpenChannelAsync(publishChannelContext).AsTask().GetAwaiter().GetResult();
        consumerChannel = consumerTransportConnection.OpenChannelAsync(new ChannelContext(consumerConnectionContext)).AsTask().GetAwaiter().GetResult();
        producerTopology = producerChannel.Topology!;
        consumerTopology = consumerChannel.Topology!;

        publishContextPool = new ContextPool<PublishContext>(() => new PublishContext(publishChannelContext));
        publishPipeline = publishPipelineBuilder.Build(services, PublishInternalAsync);
    }
    public bool IsConnected =>
        (from PersistentConnectionType type in Enum.GetValues(typeof(PersistentConnectionType)) select GetConnection(type))
        .All(connection => connection.Status.State == PersistentConnectionState.Connected);

    /// <inheritdoc />
    public PersistentConnectionStatus GetConnectionStatus(PersistentConnectionType type)
    {
        var connection = GetConnection(type);
        return connection.Status;
    }

    /// <inheritdoc />
    public async Task EnsureConnectedAsync(PersistentConnectionType type, CancellationToken cancellationToken = default)
    {
        var connection = type == PersistentConnectionType.Consumer ? consumerTransportConnection : producerTransportConnection;
        await connection.EnsureConnectedAsync(cancellationToken);
    }

    #region Consume

    /// <inheritdoc />
    public async Task<IAsyncDisposable> ConsumeAsync(Action<IConsumeConfiguration> configure)
    {
        var consumeConfiguration = new ConsumeConfiguration(configuration.PrefetchCount, handlerCollectionFactory);
        configure(consumeConfiguration);

        var channelContext = new ChannelContext(consumerConnectionContext);
        var consumerContexts = new List<ConsumerContext>();

        foreach (var (queue, handler, perQueueConfiguration) in consumeConfiguration.PerQueueConsumeConfigurations)
            consumerContexts.Add(
                CreatePerQueueContext(channelContext, queue, perQueueConfiguration, consumeConfiguration.PrefetchCount, RawHandlerTerminal(handler))
            );

        foreach (var (queue, handlers, perQueueConfiguration) in consumeConfiguration.PerQueueTypedConsumeConfigurations)
            consumerContexts.Add(
                handlers is HandlerCollection { Table: var table }
                    ? CreateTypedPerQueueContext(channelContext, queue, perQueueConfiguration, consumeConfiguration.PrefetchCount, table)
                    : CreatePerQueueContext(channelContext, queue, perQueueConfiguration, consumeConfiguration.PrefetchCount, LegacyTypedHandlerTerminal(handlers))
            );

        return await consumerChannel.StartConsumerAsync(consumerContexts);
    }

    private ConsumerContext CreatePerQueueContext(
        ChannelContext channelContext,
        in Queue queue,
        PerQueueConsumeConfiguration perQueueConfiguration,
        ushort prefetchCount,
        PipelineStep<ConsumeContext> terminal
    )
    {
        var consumerContext = new ConsumerContext(channelContext, queue.Name)
        {
            PrefetchCount = prefetchCount,
            AutoAck = perQueueConfiguration.AutoAck,
        };
        SetConsumerConfiguration(consumerContext, queue, perQueueConfiguration);
        consumerContext.MessagePipeline = consumePipelineBuilder.Build(services, terminal);
        return consumerContext;
    }

    private void SetConsumerConfiguration(ConsumerContext consumerContext, in Queue queue, PerQueueConsumeConfiguration perQueueConfiguration)
    {
        SetConsumerTelemetry(consumerContext, queue.Name);
        consumerContext.Set(RabbitKeys.Queue, queue);
        if (!string.IsNullOrEmpty(perQueueConfiguration.ConsumerTag))
            consumerContext.Set(RabbitKeys.ConsumerTag, perQueueConfiguration.ConsumerTag);
        if (perQueueConfiguration.IsExclusive)
            consumerContext.Set(RabbitKeys.ExclusiveConsumer, true);
        if (perQueueConfiguration.Arguments is { } arguments)
            consumerContext.Set(RabbitKeys.ConsumerArguments, arguments);
    }

    private void SetConsumerTelemetry(ConsumerContext consumerContext, string queue)
    {
        var telemetryOptions = services.GetService<EasyNetQ.Diagnostics.TelemetryOptions>();
        consumerContext.Set(
            Keys.ConsumerTelemetry,
            new EasyNetQ.Diagnostics.ConsumerTelemetry(queue, telemetryOptions?.MessagingSystem ?? "rabbitmq")
        );
    }

    private static PipelineStep<ConsumeContext> RawHandlerTerminal(MessageHandler handler)
        => async context => context.Ack = await handler(context.Body, context.Properties, context.ReceivedInfo, context.CancellationToken).ConfigureAwait(false);

    private ConsumerContext CreateTypedPerQueueContext(
        ChannelContext channelContext,
        in Queue queue,
        PerQueueConsumeConfiguration perQueueConfiguration,
        ushort prefetchCount,
        HandlerTable table
    )
    {
        var consumerContext = new ConsumerContext(channelContext, queue.Name)
        {
            PrefetchCount = prefetchCount,
            AutoAck = perQueueConfiguration.AutoAck,
            Handlers = table,
        };
        SetConsumerConfiguration(consumerContext, queue, perQueueConfiguration);
        consumerContext.MessagePipeline = consumePipelineBuilder.Clone()
            .Use(ResolveMessageTypeStep)
            .Use(ResolveHandlerStep)
            .Use(selectSerializerStep)
            .Use(DeserializeStep)
            .Build(services, DispatchTerminal);
        return consumerContext;
    }

    private static async ValueTask DispatchTerminal(ConsumeContext context)
        => context.Ack = await context.Handler!.InvokeAsync(context).ConfigureAwait(false);

    private PipelineStep<ConsumeContext> LegacyTypedHandlerTerminal(IHandlerCollection handlers)
        => async context =>
        {
            var message = messageSerializationStrategy.DeserializeMessage(context.Properties, context.Body);
            var handler = handlers.GetHandler(message.MessageType);
            context.Ack = await handler(message, context.ReceivedInfo, context.CancellationToken).ConfigureAwait(false);
        };

    /// <inheritdoc />
    public IPullingConsumer<PullResult> CreatePullingConsumer(in Queue queue, bool autoAck = true)
    {
        var options = new PullingConsumerOptions(autoAck, configuration.Timeout);
        return pullingConsumerFactory.CreateConsumer(queue, options);
    }

    /// <inheritdoc />
    public IPullingConsumer<PullResult<T>> CreatePullingConsumer<T>(in Queue queue, bool autoAck = true)
    {
        var options = new PullingConsumerOptions(autoAck, configuration.Timeout);
        return pullingConsumerFactory.CreateConsumer<T>(queue, options);
    }

    #endregion


    /// <inheritdoc />
    public event EventHandler<ConnectedEventArgs> Connected;

    /// <inheritdoc />
    public event EventHandler<DisconnectedEventArgs> Disconnected;

    /// <inheritdoc />
    public event EventHandler<BlockedEventArgs> Blocked;

    /// <inheritdoc />
    public event EventHandler<UnblockedEventArgs> Unblocked;

    /// <inheritdoc />
    public event EventHandler<MessageReturnedEventArgs> MessageReturned;

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (disposed) return;

        disposed = true;

        foreach (var eventSubscription in eventSubscriptions)
            eventSubscription.Dispose();

        Connected -= advancedBusEventHandlers.Connected;
        Disconnected -= advancedBusEventHandlers.Disconnected;
        Blocked -= advancedBusEventHandlers.Blocked;
        Unblocked -= advancedBusEventHandlers.Unblocked;
        MessageReturned -= advancedBusEventHandlers.MessageReturned;
    }

    #region Publish

    /// <inheritdoc />
    public virtual async Task PublishAsync(
        string exchange,
        string routingKey,
        bool? mandatory,
        bool? publisherConfirms,
        IMessage message,
        CancellationToken cancellationToken
    )
    {
        using var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
        await PublishAsync(
            exchange, routingKey, mandatory, publisherConfirms, serializedMessage.Properties, serializedMessage.Body, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync<T>(
        string exchange,
        string routingKey,
        bool? mandatory,
        bool? publisherConfirms,
        MessageProperties properties,
        T body,
        CancellationToken cancellationToken
    )
    {
        using var serializedMessage = messageSerializationStrategy.SerializeMessage(body, properties);
        await PublishAsync(
            exchange, routingKey, mandatory, publisherConfirms, serializedMessage.Properties, serializedMessage.Body, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync(
        string exchange,
        string routingKey,
        bool? mandatory,
        bool? publisherConfirms,
        MessageProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var context = publishContextPool.Rent();
        try
        {
            context.Exchange = exchange;
            context.RoutingKey = routingKey;
            context.Mandatory = mandatory ?? configuration.MandatoryPublish;
            context.PublisherConfirms = publisherConfirms ?? configuration.PublisherConfirms;
            context.Properties = properties;
            context.Body = body;
            context.CancellationToken = cts.Token;

            await publishPipeline(context).ConfigureAwait(false);
        }
        finally
        {
            publishContextPool.Return(context);
        }
    }

    #endregion

    #region Topology

    /// <inheritdoc />
    public async Task<QueueStats> GetQueueStatsAsync(string queue, CancellationToken cancellationToken)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var stats = await consumerTopology.GetQueueStatsAsync(queue, cts.Token).ConfigureAwait(false);

        logger.QueueStatsRetrieved(queue, (uint)stats.MessagesCount, (uint)stats.ConsumersCount);

        return stats;
    }

    /// <inheritdoc />
    public async Task QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await consumerTopology.DeclareQueuePassiveAsync(queue, cts.Token).ConfigureAwait(false);

        logger.QueueDeclaredPassive(queue);
    }

    /// <inheritdoc />
    public async Task<Queue> QueueDeclareAsync(
        string queue,
        Action<IQueueDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var options = new QueueDeclareConfiguration();
        configure?.Invoke(options);
        var topology = options.PersistentConnectionType == PersistentConnectionType.Consumer ? consumerTopology : producerTopology;
        var queueName = await topology.DeclareQueueAsync(
            new QueueDefinition(queue, options.IsDurable, options.IsExclusive, options.IsAutoDelete) { Arguments = options.Arguments },
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.QueueDeclared(
                queueName,
                options.IsDurable,
                options.IsExclusive,
                options.IsAutoDelete,
                options.Arguments?.Stringify()
            );
        }

        return new Queue(queueName, options.IsDurable, options.IsExclusive, options.IsAutoDelete, options.Arguments);
    }

    /// <inheritdoc />
    public virtual async Task QueueDeleteAsync(
        string queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await consumerTopology.DeleteQueueAsync(queue, ifUnused, ifEmpty, cts.Token).ConfigureAwait(false);

        logger.QueueDeleted(queue);
    }

    /// <inheritdoc />
    public virtual async Task QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await consumerTopology.PurgeQueueAsync(queue, cts.Token).ConfigureAwait(false);

        logger.QueuePurged(queue);
    }

    /// <inheritdoc />
    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await producerTopology.DeclareExchangePassiveAsync(exchange, cts.Token).ConfigureAwait(false);

        logger.ExchangeDeclaredPassive(exchange);
    }

    /// <inheritdoc />
    public async Task<Exchange> ExchangeDeclareAsync(
        string exchange,
        string type,
        bool durable,
        bool autoDelete,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        IDictionary<string, object> nullableArguments = arguments?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

        await producerTopology.DeclareExchangeAsync(
            new ExchangeDefinition(exchange, type, durable, autoDelete) { Arguments = nullableArguments }, cts.Token
        ).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.ExchangeDeclared(
                exchange,
                type,
                durable,
                autoDelete,
                arguments?.Stringify()
            );
        }

        return new Exchange(exchange, type, durable, autoDelete, arguments);
    }

    /// <inheritdoc />
    public virtual async Task ExchangeDeleteAsync(
        string exchange, bool ifUnused = false, CancellationToken cancellationToken = default
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await producerTopology.DeleteExchangeAsync(exchange, ifUnused, cts.Token).ConfigureAwait(false);

        logger.ExchangeDeleted(exchange);
    }

    /// <inheritdoc />
    public virtual async Task QueueBindAsync(
        string queue,
        string exchange,
        string routingKey,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        IDictionary<string, object> nullableArguments = arguments?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

        await consumerTopology.BindAsync(
            new BindingDefinition(exchange, queue, routingKey) { Arguments = nullableArguments }, cts.Token
        ).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.QueueBound(
                queue,
                exchange,
                routingKey,
                arguments?.Stringify()
            );
        }
    }

    /// <inheritdoc />
    public virtual async Task QueueUnbindAsync(
        string queue,
        string exchange,
        string routingKey,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        IDictionary<string, object> nullableArguments = arguments?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

        await consumerTopology.UnbindAsync(
            new BindingDefinition(exchange, queue, routingKey) { Arguments = nullableArguments }, cts.Token
        ).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.QueueUnbound(
                queue,
                exchange,
                routingKey,
                arguments?.Stringify()
            );
        }
    }

    /// <inheritdoc />
    public virtual async Task ExchangeBindAsync(
        string destinationExchange,
        string sourceExchange,
        string routingKey,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        IDictionary<string, object> nullableArguments = arguments?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

        await producerTopology.BindAsync(
            new BindingDefinition(sourceExchange, destinationExchange, routingKey, DestinationIsExchange: true) { Arguments = nullableArguments },
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.ExchangeBound(
                destinationExchange,
                sourceExchange,
                routingKey,
                arguments?.Stringify()
            );
        }
    }

    /// <inheritdoc />
    public virtual async Task ExchangeUnbindAsync(
        string destinationExchange,
        string sourceExchange,
        string routingKey,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        IDictionary<string, object> nullableArguments = arguments?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

        await producerTopology.UnbindAsync(
            new BindingDefinition(sourceExchange, destinationExchange, routingKey, DestinationIsExchange: true) { Arguments = nullableArguments },
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.ExchangeUnbound(
                destinationExchange,
                sourceExchange,
                routingKey,
                arguments?.Stringify()
            );
        }
    }

    #endregion

    private IPersistentConnection GetConnection(PersistentConnectionType type) =>
        type switch
        {
            PersistentConnectionType.Producer => producerConnection,
            PersistentConnectionType.Consumer => consumerConnection,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    private Task OnConnectionCreated(ConnectionCreatedEvent messageEvent)
    {
        Connected?.Invoke(
            this,
            new ConnectedEventArgs(messageEvent.Type, messageEvent.Endpoint.HostName, messageEvent.Endpoint.Port)
        );
        return Task.CompletedTask;
    }

    private Task OnConnectionRecovered(ConnectionRecoveredEvent messageEvent)
    {
        Connected?.Invoke(
            this,
            new ConnectedEventArgs(messageEvent.Type, messageEvent.Endpoint.HostName, messageEvent.Endpoint.Port)
        );
        return Task.CompletedTask;
    }

    private Task OnConnectionDisconnected(ConnectionDisconnectedEvent messageEvent)
    {
        Disconnected?.Invoke(
            this,
            new DisconnectedEventArgs(messageEvent.Type, messageEvent.Endpoint.HostName, messageEvent.Endpoint.Port, messageEvent.Reason)
        );
        return Task.CompletedTask;
    }

    private Task OnConnectionBlocked(ConnectionBlockedEvent messageEvent)
    {
        Blocked?.Invoke(this, new BlockedEventArgs(messageEvent.Type, messageEvent.Reason));
        return Task.CompletedTask;
    }

    private Task OnConnectionUnblocked(ConnectionUnblockedEvent messageEvent)
    {
        Unblocked?.Invoke(this, new UnblockedEventArgs(messageEvent.Type));
        return Task.CompletedTask;
    }

    private Task OnMessageReturned(ReturnedMessageEvent messageEvent)
    {
        MessageReturned?.Invoke(this, new MessageReturnedEventArgs(messageEvent.Body, messageEvent.Properties, messageEvent.Info));
        return Task.CompletedTask;
    }

    private ValueTask PublishInternalAsync(PublishContext context) => producerChannel.PublishAsync(context);
}
