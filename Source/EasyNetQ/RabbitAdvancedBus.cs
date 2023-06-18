using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Logging;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ;

/// <inheritdoc cref="IAdvancedBus"/>
public class RabbitAdvancedBus : IAdvancedBus, IDisposable
{
    private readonly IPersistentChannelDispatcher persistentChannelDispatcher;
    private readonly ConnectionConfiguration configuration;
    private readonly ConsumePipelineBuilder consumePipelineBuilder;
    private readonly IServiceResolver serviceResolver;
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
        IServiceResolver serviceResolver,
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

        eventSubscriptions = new[]
        {
            this.eventBus.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated),
            this.eventBus.Subscribe<ConnectionRecoveredEvent>(OnConnectionRecovered),
            this.eventBus.Subscribe<ConnectionDisconnectedEvent>(OnConnectionDisconnected),
            this.eventBus.Subscribe<ConnectionBlockedEvent>(OnConnectionBlocked),
            this.eventBus.Subscribe<ConnectionUnblockedEvent>(OnConnectionUnblocked),
            this.eventBus.Subscribe<ReturnedMessageEvent>(OnMessageReturned),
        };

        produceDelegate = producePipelineBuilder.Use(_ => PublishInternalAsync).Build();
    }


    /// <inheritdoc />
    public bool IsConnected => producerConnection.IsConnected && consumerConnection.IsConnected;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        producerConnection.Connect();
        consumerConnection.Connect();
        return Task.CompletedTask;
    }

    #region Consume

    /// <inheritdoc />
    public IDisposable Consume(Action<IConsumeConfiguration> configure)
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
                        _ => ctx => new ValueTask<AckStrategy>(x.Item2(ctx.Body, ctx.Properties, ctx.ReceivedInfo, ctx.CancellationToken))
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
                                return new ValueTask<AckStrategy>(handler(deserializedMessage, ctx.ReceivedInfo, ctx.CancellationToken));
                            }
                        ).Build()
                    )
                )
            ).ToDictionary(x => x.Key, x => x.Value)
        );
        var consumer = consumerFactory.CreateConsumer(consumerConfiguration);
        consumer.StartConsuming();
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
    public event EventHandler<ConnectedEventArgs>? Connected;

    /// <inheritdoc />
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    /// <inheritdoc />
    public event EventHandler<BlockedEventArgs>? Blocked;

    /// <inheritdoc />
    public event EventHandler<UnblockedEventArgs>? Unblocked;

    /// <inheritdoc />
    public event EventHandler<MessageReturnedEventArgs>? MessageReturned;

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

        await produceDelegate(new ProduceContext(exchange, routingKey, mandatory ?? configuration.MandatoryPublish, publisherConfirms ?? configuration.PublisherConfirms, properties, body, serviceResolver, cts.Token)).ConfigureAwait(false);
    }

    #endregion

    #region Topology

    /// <inheritdoc />
    public async Task<QueueStats> GetQueueStatsAsync(string queue, CancellationToken cancellationToken)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var declareResult = await persistentChannelDispatcher.InvokeAsync(
            x => x.QueueDeclarePassive(queue), PersistentChannelDispatchOptions.ConsumerTopology, cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "{messagesCount} messages, {consumersCount} consumers in queue {queue}",
                declareResult.MessageCount,
                declareResult.ConsumerCount,
                queue
            );
        }

        return new QueueStats(declareResult.MessageCount, declareResult.ConsumerCount);
    }

    /// <inheritdoc />
    public async Task QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.QueueDeclarePassive(queue), PersistentChannelDispatchOptions.ConsumerTopology, cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat("Passive declared queue {queue}", queue);
        }
    }

    /// <inheritdoc />
    public async Task<Queue> QueueDeclareAsync(
        string queue,
        bool durable,
        bool exclusive,
        bool autoDelete,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var queueDeclareOk = await persistentChannelDispatcher.InvokeAsync(
            x => x.QueueDeclare(queue, durable, exclusive, autoDelete, arguments),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Declared queue {queue}: durable={durable}, exclusive={exclusive}, autoDelete={autoDelete}, arguments={arguments}",
                queueDeclareOk.QueueName,
                durable,
                exclusive,
                autoDelete,
                arguments?.Stringify()
            );
        }

        return new Queue(queueDeclareOk.QueueName, durable, exclusive, autoDelete, arguments);
    }

    /// <inheritdoc />
    public virtual async Task QueueDeleteAsync(
        string queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.QueueDelete(queue, ifUnused, ifEmpty),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat("Deleted queue {queue}", queue);
        }
    }

    /// <inheritdoc />
    public virtual async Task QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.QueuePurge(queue),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat("Purged queue {queue}", queue);
        }
    }

    /// <inheritdoc />
    public async Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.ExchangeDeclarePassive(exchange), PersistentChannelDispatchOptions.ProducerTopology, cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat("Passive declared exchange {exchange}", exchange);
        }
    }

    /// <inheritdoc />
    public async Task<Exchange> ExchangeDeclareAsync(
        string exchange,
        string type,
        bool durable,
        bool autoDelete,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.ExchangeDeclare(exchange, type, durable, autoDelete, arguments),
            PersistentChannelDispatchOptions.ProducerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Declared exchange {exchange}: type={type}, durable={durable}, autoDelete={autoDelete}, arguments={arguments}",
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
            x => x.ExchangeDelete(exchange, ifUnused), PersistentChannelDispatchOptions.ProducerTopology, cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat("Deleted exchange {exchange}", exchange);
        }
    }

    /// <inheritdoc />
    public virtual async Task QueueBindAsync(
        string queue,
        string exchange,
        string routingKey,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.QueueBind(queue, exchange, routingKey, arguments),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Bound queue {queue} to exchange {exchange} with routing key {routingKey} and arguments {arguments}",
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
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.QueueUnbind(queue, exchange, routingKey, arguments),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Unbound queue {queue} from exchange {exchange} with routing key {routingKey} and arguments {arguments}",
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
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.ExchangeBind(destinationExchange, sourceExchange, routingKey, arguments),
            PersistentChannelDispatchOptions.ProducerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Bound destination exchange {destinationExchange} to source exchange {sourceExchange} with routing key {routingKey} and arguments {arguments}",
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
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        await persistentChannelDispatcher.InvokeAsync(
            x => x.ExchangeUnbind(destinationExchange, sourceExchange, routingKey, arguments),
            PersistentChannelDispatchOptions.ProducerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Unbound destination exchange {destinationExchange} from source exchange {sourceExchange} with routing key {routingKey} and arguments {arguments}",
                destinationExchange,
                sourceExchange,
                routingKey,
                arguments?.Stringify()
            );
        }
    }

    #endregion


    private void OnConnectionCreated(in ConnectionCreatedEvent @event)
    {
        Connected?.Invoke(
            this,
            new ConnectedEventArgs(@event.Type, @event.Endpoint.HostName, @event.Endpoint.Port)
        );
    }

    private void OnConnectionRecovered(in ConnectionRecoveredEvent @event)
    {
        Connected?.Invoke(
            this,
            new ConnectedEventArgs(@event.Type, @event.Endpoint.HostName, @event.Endpoint.Port)
        );
    }

    private void OnConnectionDisconnected(in ConnectionDisconnectedEvent @event)
    {
        Disconnected?.Invoke(
            this,
            new DisconnectedEventArgs(@event.Type, @event.Endpoint.HostName, @event.Endpoint.Port, @event.Reason)
        );
    }

    private void OnConnectionBlocked(in ConnectionBlockedEvent @event)
    {
        Blocked?.Invoke(this, new BlockedEventArgs(@event.Type, @event.Reason));
    }

    private void OnConnectionUnblocked(in ConnectionUnblockedEvent @event)
    {
        Unblocked?.Invoke(this, new UnblockedEventArgs(@event.Type));
    }

    private void OnMessageReturned(in ReturnedMessageEvent @event)
    {
        MessageReturned?.Invoke(this, new MessageReturnedEventArgs(@event.Body, @event.Properties, @event.Info));
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

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Published to exchange {exchange} with routingKey={routingKey} and correlationId={correlationId}",
                context.Exchange,
                context.RoutingKey,
                context.Properties.CorrelationId
            );
        }

        eventBus.Publish(
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

        public bool Invoke(IModel model)
        {
            var basicProperties = model.CreateBasicProperties();
            properties.CopyTo(basicProperties);
            model.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);
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

        public IPublishPendingConfirmation Invoke(IModel model)
        {
            var confirmation = confirmationListener.CreatePendingConfirmation(model);
            var basicProperties = model.CreateBasicProperties();
            properties.SetConfirmationId(confirmation.Id).CopyTo(basicProperties);
            try
            {
                model.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);
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
