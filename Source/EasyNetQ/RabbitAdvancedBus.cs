using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ;

/// <inheritdoc cref="IAdvancedBus"/>
public class RabbitAdvancedBus : IAdvancedBus, IDisposable
{
    private readonly IPersistentChannelDispatcher persistentChannelDispatcher;
    private readonly ConnectionConfiguration configuration;
    private readonly ConsumePipelineBuilder consumePipelineBuilder;
    private readonly IServiceProvider serviceResolver;
    private readonly IPublishConfirmationListener confirmationListener;
    private readonly ILogger logger;
    private readonly IProducerConnection producerConnection;
    private readonly IConsumerConnection consumerConnection;
    private readonly IConsumerFactory consumerFactory;
    private readonly IEventBus eventBus;
    private readonly IDisposable[] eventSubscriptions;
    private readonly IHandlerCollectionFactory handlerCollectionFactory;
    private readonly IMessageSerializationStrategy messageSerializationStrategy;
    private readonly IPullingConsumerFactory pullingConsumerFactory;
    private readonly AdvancedBusEventHandlers advancedBusEventHandlers;

    private volatile bool disposed;
    private readonly ProduceDelegate produceDelegate;

    /// <summary>
    ///     Creates RabbitAdvancedBus
    /// </summary>
    public RabbitAdvancedBus(
        ILogger<RabbitAdvancedBus> logger,
        IProducerConnection producerConnection,
        IConsumerConnection consumerConnection,
        IConsumerFactory consumerFactory,
        IPersistentChannelDispatcher persistentChannelDispatcher,
        IPublishConfirmationListener confirmationListener,
        IEventBus eventBus,
        IHandlerCollectionFactory handlerCollectionFactory,
        ConnectionConfiguration configuration,
        ProducePipelineBuilder producePipelineBuilder,
        ConsumePipelineBuilder consumePipelineBuilder,
        IServiceProvider serviceResolver,
        IMessageSerializationStrategy messageSerializationStrategy,
        IPullingConsumerFactory pullingConsumerFactory,
        AdvancedBusEventHandlers advancedBusEventHandlers
    )
    {
        this.logger = logger;
        this.producerConnection = producerConnection;
        this.consumerConnection = consumerConnection;
        this.consumerFactory = consumerFactory;
        this.persistentChannelDispatcher = persistentChannelDispatcher;
        this.confirmationListener = confirmationListener;
        this.eventBus = eventBus;
        this.handlerCollectionFactory = handlerCollectionFactory;
        this.configuration = configuration;
        this.consumePipelineBuilder = consumePipelineBuilder;
        this.serviceResolver = serviceResolver;
        this.messageSerializationStrategy = messageSerializationStrategy;
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

        produceDelegate = producePipelineBuilder.Use(_ => PublishInternalAsync).Build();
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

        var consumerConfiguration = new ConsumerConfiguration(
            consumeConfiguration.PrefetchCount,
            consumeConfiguration.PerQueueConsumeConfigurations.ToDictionary(
                x => x.Item1,
                x => new PerQueueConsumerConfiguration(
                    x.Item3.AutoAck,
                    x.Item3.ConsumerTag,
                    x.Item3.IsExclusive,
                    x.Item3.Arguments,
                    consumePipelineBuilder.Use(
                        _ => ctx => new ValueTask<AckStrategyAsync>(x.Item2(ctx.Body, ctx.Properties, ctx.ReceivedInfo, ctx.CancellationToken))
                    ).Build()
                )).Union(
                consumeConfiguration.PerQueueTypedConsumeConfigurations.ToDictionary(
                    x => x.Item1,
                    x => new PerQueueConsumerConfiguration(
                        x.Item3.AutoAck,
                        x.Item3.ConsumerTag,
                        x.Item3.IsExclusive,
                        x.Item3.Arguments,
                        consumePipelineBuilder.Use(
                            _ => ctx =>
                            {
                                var deserializedMessage = messageSerializationStrategy.DeserializeMessage(ctx.Properties, ctx.Body);
                                var handler = x.Item2.GetHandler(deserializedMessage.MessageType);
                                return new ValueTask<AckStrategyAsync>(handler(deserializedMessage, ctx.ReceivedInfo, ctx.CancellationToken));
                            }
                        ).Build()
                    )
                )
            ).ToDictionary(x => x.Key, x => x.Value)
        );
        var consumer = consumerFactory.CreateConsumer(consumerConfiguration);
        await consumer.StartConsumingAsync();
        return consumer;
    }

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

        await produceDelegate(new ProduceContext(exchange, routingKey, mandatory ?? configuration.MandatoryPublish,
            publisherConfirms ?? configuration.PublisherConfirms, properties, body, serviceResolver, cts.Token)).ConfigureAwait(false);
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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "{Queue} has {MessagesCount} messages and {ConsumersCount} consumers.",
                queue,
                declareResult.MessageCount,
                declareResult.ConsumerCount
            );
        }

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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Passive declared queue {Queue}", queue);
        }
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
            logger.LogDebug(
                "Declared queue {Queue}: durable={Durable}, exclusive={Exclusive}, autoDelete={AutoDelete}, arguments={Arguments}",
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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Deleted queue {Queue}", queue);
        }
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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Purged queue {Queue}", queue);
        }
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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Passive declared exchange {Exchange}", exchange);
        }
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
            logger.LogDebug(
                "Declared exchange {Exchange}: type={Type}, durable={Durable}, autoDelete={AutoDelete}, arguments={Arguments}",
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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Deleted exchange {Exchange}", exchange);
        }
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
            logger.LogDebug(
                "Bound queue {Queue} to exchange {Exchange} with routing key {RoutingKey} and arguments {Arguments}",
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
            logger.LogDebug(
                "Unbound queue {Queue} from exchange {Exchange} with routing key {RoutingKey} and arguments {Arguments}",
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
            logger.LogDebug(
                "Bound destination exchange {DestinationExchange} to source exchange {SourceExchange} with routing key {RoutingKey} and arguments {Arguments}",
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
            logger.LogDebug(
                "Unbound destination exchange {DestinationExchange} from source exchange {SourceExchange} with routing key {RoutingKey} and arguments {Arguments}",
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

    private async ValueTask PublishInternalAsync(ProduceContext context)
    {
        if (context.PublisherConfirms)
        {
            while (true)
            {
                var pendingConfirmation = await persistentChannelDispatcher.InvokeAsync<IPublishPendingConfirmation, BasicPublishWithConfirmsAction>(
                    new BasicPublishWithConfirmsAction(
                        confirmationListener, context.Exchange, context.RoutingKey, context.Mandatory, context.Properties, context.Body
                    ),
                    PersistentChannelDispatchOptions.ProducerPublishWithConfirms,
                    context.CancellationToken
                ).ConfigureAwait(false);

                try
                {
                    await pendingConfirmation.WaitAsync(context.CancellationToken).ConfigureAwait(false);
                    break;
                }
                catch (PublishInterruptedException)
                {
                }
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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Published to exchange {Exchange} with routingKey={RoutingKey} and correlationId={CorrelationId}",
                context.Exchange,
                context.RoutingKey,
                context.Properties.CorrelationId
            );
        }

        await eventBus.PublishAsync(
            new PublishedMessageEvent(context.Exchange, context.RoutingKey, context.Properties, context.Body)
        );
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

    private readonly struct BasicPublishWithConfirmsAction : IPersistentChannelAction<IPublishPendingConfirmation>
    {
        private readonly IPublishConfirmationListener confirmationListener;
        private readonly string exchange;
        private readonly string routingKey;
        private readonly bool mandatory;
        private readonly MessageProperties properties;
        private readonly ReadOnlyMemory<byte> body;

        public BasicPublishWithConfirmsAction(
            IPublishConfirmationListener confirmationListener,
            string exchange,
            string routingKey,
            bool mandatory,
            in MessageProperties properties,
            in ReadOnlyMemory<byte> body
        )
        {
            this.confirmationListener = confirmationListener;
            this.exchange = exchange;
            this.routingKey = routingKey;
            this.mandatory = mandatory;
            this.properties = properties;
            this.body = body;
        }

        public async Task<IPublishPendingConfirmation> InvokeAsync(IChannel channel, CancellationToken cancellationToken = default)
        {
            var confirmation = await confirmationListener.CreatePendingConfirmationAsync(channel, cancellationToken);
            var basicProperties = new BasicProperties();
            properties.SetConfirmationId(confirmation.Id).CopyTo(basicProperties);

            try
            {
                await channel.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, cancellationToken);
            }
            catch (Exception)
            {
                confirmation.Cancel();
                throw;
            }

            return confirmation;
        }
    }
}
