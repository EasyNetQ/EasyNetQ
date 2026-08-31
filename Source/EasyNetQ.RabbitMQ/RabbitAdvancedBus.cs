using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ;

/// <inheritdoc cref="IAdvancedBus"/>
public class RabbitAdvancedBus : IAdvancedBus, IDisposable
{
    private readonly IPersistentChannelDispatcher persistentChannelDispatcher;
    private readonly ConnectionConfiguration configuration;
    private readonly PipelineBuilder<ConsumeContext> consumePipelineBuilder;
    private readonly IServiceProvider services;
    private readonly ILogger logger;
    private readonly IProducerConnection producerConnection;
    private readonly IConsumerConnection consumerConnection;
    private readonly IConsumerFactory consumerFactory;
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
        IConsumerFactory consumerFactory,
        IPersistentChannelDispatcher persistentChannelDispatcher,
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
        this.consumerFactory = consumerFactory;
        this.persistentChannelDispatcher = persistentChannelDispatcher;
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
        consumerConnectionContext = new ConnectionContext("Consumer", services);
        var publishChannelContext = new ChannelContext(producerConnectionContext);
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
        var connection = GetConnection(type);
        await connection.ConnectAsync();
    }

    #region Consume

    /// <inheritdoc />
    public async Task<IAsyncDisposable> ConsumeAsync(Action<IConsumeConfiguration> configure)
    {
        var consumeConfiguration = new ConsumeConfiguration(configuration.PrefetchCount, handlerCollectionFactory);
        configure(consumeConfiguration);

        var channelContext = new ChannelContext(consumerConnectionContext);
        var perQueueConfigurations = new Dictionary<Queue, PerQueueConsumerConfiguration>();

        foreach (var (queue, handler, perQueueConfiguration) in consumeConfiguration.PerQueueConsumeConfigurations)
            perQueueConfigurations.Add(
                queue,
                CreatePerQueueConfiguration(channelContext, queue, perQueueConfiguration, consumeConfiguration.PrefetchCount, RawHandlerTerminal(handler))
            );

        foreach (var (queue, handlers, perQueueConfiguration) in consumeConfiguration.PerQueueTypedConsumeConfigurations)
            perQueueConfigurations.Add(
                queue,
                handlers is HandlerCollection { Table: var table }
                    ? CreateTypedPerQueueConfiguration(channelContext, queue, perQueueConfiguration, consumeConfiguration.PrefetchCount, table)
                    : CreatePerQueueConfiguration(channelContext, queue, perQueueConfiguration, consumeConfiguration.PrefetchCount, LegacyTypedHandlerTerminal(handlers))
            );

        var consumerConfiguration = new ConsumerConfiguration(consumeConfiguration.PrefetchCount, perQueueConfigurations);
        var consumer = consumerFactory.CreateConsumer(consumerConfiguration);
        await consumer.StartConsumingAsync();
        return consumer;
    }

    private PerQueueConsumerConfiguration CreatePerQueueConfiguration(
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
        SetConsumerTelemetry(consumerContext, queue.Name);
        consumerContext.MessagePipeline = consumePipelineBuilder.Build(services, terminal);
        return new PerQueueConsumerConfiguration(
            perQueueConfiguration.AutoAck,
            perQueueConfiguration.ConsumerTag,
            perQueueConfiguration.IsExclusive,
            perQueueConfiguration.Arguments,
            consumerContext
        );
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

    private PerQueueConsumerConfiguration CreateTypedPerQueueConfiguration(
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
        SetConsumerTelemetry(consumerContext, queue.Name);
        consumerContext.MessagePipeline = consumePipelineBuilder.Clone()
            .Use(ResolveMessageTypeStep)
            .Use(ResolveHandlerStep)
            .Use(selectSerializerStep)
            .Use(DeserializeStep)
            .Build(services, DispatchTerminal);
        return new PerQueueConsumerConfiguration(
            perQueueConfiguration.AutoAck,
            perQueueConfiguration.ConsumerTag,
            perQueueConfiguration.IsExclusive,
            perQueueConfiguration.Arguments,
            consumerContext
        );
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

        var declareResult = await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueDeclarePassiveAsync(queue, cancellationToken),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        logger.QueueStatsRetrieved(queue, declareResult.MessageCount, declareResult.ConsumerCount);

        return new QueueStats(declareResult.MessageCount, declareResult.ConsumerCount);
    }

    /// <inheritdoc />
    public async Task QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueDeclarePassiveAsync(queue, cancellationToken),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

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
        var dispatchOptions = options.PersistentConnectionType == PersistentConnectionType.Consumer ? PersistentChannelDispatchOptions.ConsumerTopology : PersistentChannelDispatchOptions.ProducerTopology;
        var declareResult = await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueDeclareAsync(queue, options.IsDurable, options.IsExclusive, options.IsAutoDelete, options.Arguments, cancellationToken: cancellationToken),
            dispatchOptions,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.QueueDeclared(
                declareResult.QueueName,
                options.IsDurable,
                options.IsExclusive,
                options.IsAutoDelete,
                options.Arguments?.Stringify()
            );
        }

        return new Queue(declareResult.QueueName, options.IsDurable, options.IsExclusive, options.IsAutoDelete, options.Arguments);
    }

    /// <inheritdoc />
    public virtual async Task QueueDeleteAsync(
        string queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueDeleteAsync(queue, ifUnused, ifEmpty, cancellationToken: cancellationToken),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        logger.QueueDeleted(queue);
    }

    /// <inheritdoc />
    public virtual async Task QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueuePurgeAsync(queue, cancellationToken),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        logger.QueuePurged(queue);
    }

    /// <inheritdoc />
    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.ExchangeDeclarePassiveAsync(exchange, cancellationToken),
            PersistentChannelDispatchOptions.ProducerTopology,
            cts.Token
        ).ConfigureAwait(false);

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

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.ExchangeDeclareAsync(exchange, type, durable, autoDelete, nullableArguments, cancellationToken: cancellationToken),
            PersistentChannelDispatchOptions.ProducerTopology,
            cts.Token
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

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.ExchangeDeleteAsync(exchange, ifUnused, cancellationToken: cancellationToken),
            PersistentChannelDispatchOptions.ProducerTopology,
            cts.Token
        ).ConfigureAwait(false);

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

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueBindAsync(queue, exchange, routingKey, nullableArguments, cancellationToken: cancellationToken),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
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

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.QueueUnbindAsync(queue, exchange, routingKey, nullableArguments, cancellationToken),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
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

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.ExchangeBindAsync(destinationExchange, sourceExchange, routingKey, nullableArguments, cancellationToken: cancellationToken),
            PersistentChannelDispatchOptions.ProducerTopology,
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

        await persistentChannelDispatcher.InvokeAsync(
            async x => await x.ExchangeUnbindAsync(destinationExchange, sourceExchange, routingKey, nullableArguments, cancellationToken: cancellationToken),
            PersistentChannelDispatchOptions.ProducerTopology,
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

    private async ValueTask PublishInternalAsync(PublishContext context)
    {
        if (context.PublisherConfirms)
        {
            // The action starts the publish inside the channel mutex and hands back the in-flight task; the
            // client-side confirmation tracking completes it when the broker confirms. Awaiting it here, outside
            // the mutex, keeps confirmed publishes concurrent (bounded by the channel's rate limiter).
            var publishTask = await persistentChannelDispatcher.InvokeAsync<Task, StartConfirmedPublishAction>(
                new StartConfirmedPublishAction(context.Exchange, context.RoutingKey, context.Mandatory, context.Properties, context.Body),
                PersistentChannelDispatchOptions.ProducerPublishWithConfirms,
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
                PersistentChannelDispatchOptions.ProducerPublish,
                context.CancellationToken
            ).ConfigureAwait(false);
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
            string exchange,
            string routingKey,
            bool mandatory,
            in MessageProperties properties,
            in ReadOnlyMemory<byte> body
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

    private readonly struct StartConfirmedPublishAction : IPersistentChannelAction<Task>
    {
        private readonly string exchange;
        private readonly string routingKey;
        private readonly bool mandatory;
        private readonly MessageProperties properties;
        private readonly ReadOnlyMemory<byte> body;

        public StartConfirmedPublishAction(
            string exchange,
            string routingKey,
            bool mandatory,
            in MessageProperties properties,
            in ReadOnlyMemory<byte> body
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
}
