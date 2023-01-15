using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Interception;
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
    private readonly IPublishConfirmationListener confirmationListener;
    private readonly ILogger logger;
    private readonly IProducerConnection producerConnection;
    private readonly IConsumerConnection consumerConnection;
    private readonly IConsumerFactory consumerFactory;
    private readonly IEventBus eventBus;
    private readonly IDisposable[] eventSubscriptions;
    private readonly IHandlerCollectionFactory handlerCollectionFactory;
    private readonly IMessageSerializationStrategy messageSerializationStrategy;
    private readonly IProduceConsumeInterceptor[] produceConsumeInterceptors;
    private readonly IPullingConsumerFactory pullingConsumerFactory;
    private readonly AdvancedBusEventHandlers advancedBusEventHandlers;
    private readonly IConsumeScopeProvider consumeScopeProvider;

    private volatile bool disposed;

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
        IEnumerable<IProduceConsumeInterceptor> produceConsumeInterceptors,
        IMessageSerializationStrategy messageSerializationStrategy,
        IPullingConsumerFactory pullingConsumerFactory,
        AdvancedBusEventHandlers advancedBusEventHandlers,
        IConsumeScopeProvider consumeScopeProvider
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
        this.produceConsumeInterceptors = produceConsumeInterceptors.ToArray();
        this.messageSerializationStrategy = messageSerializationStrategy;
        this.pullingConsumerFactory = pullingConsumerFactory;
        this.advancedBusEventHandlers = advancedBusEventHandlers;
        this.consumeScopeProvider = consumeScopeProvider;

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
        var consumeConfiguration = new ConsumeConfiguration(
            configuration.PrefetchCount, handlerCollectionFactory
        );
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
                    async (body, properties, receivedInfo, cancellationToken) =>
                    {
                        var rawMessage = produceConsumeInterceptors.OnConsume(
                            new ConsumedMessage(receivedInfo, properties, body)
                        );
                        await using var scope = consumeScopeProvider.CreateAsyncScope().ConfigureAwait(false);
                        return await x.Item2(
                            rawMessage.Body, rawMessage.Properties, rawMessage.ReceivedInfo, cancellationToken
                        ).ConfigureAwait(false);
                    }
                )
            ).Union(
                consumeConfiguration.PerQueueTypedConsumeConfigurations.ToDictionary(
                    x => x.Item1,
                    x => new PerQueueConsumerConfiguration(
                        x.Item3.AutoAck,
                        x.Item3.ConsumerTag,
                        x.Item3.IsExclusive,
                        x.Item3.Arguments,
                        async (body, properties, receivedInfo, cancellationToken) =>
                        {
                            var rawMessage = produceConsumeInterceptors.OnConsume(
                                new ConsumedMessage(receivedInfo, properties, body)
                            );
                            var deserializedMessage = messageSerializationStrategy.DeserializeMessage(
                                rawMessage.Properties, rawMessage.Body
                            );
                            var handler = x.Item2.GetHandler(deserializedMessage.MessageType);
                            await using var scope = consumeScopeProvider.CreateAsyncScope().ConfigureAwait(false);
                            return await handler(deserializedMessage, receivedInfo, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    )
                )
            ).ToDictionary(x => x.Key, x => x.Value)
        );
        var consumer = consumerFactory.CreateConsumer(consumerConfiguration);
        consumer.StartConsuming();
        return consumer;
    }

    #endregion

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
    public virtual async Task PublishAsync<TMessage>(
        Exchange exchange,
        string routingKey,
        bool mandatory,
        TMessage message,
        CancellationToken cancellationToken
    ) where TMessage: IMessage
    {
        using var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
        await PublishAsync(
            exchange, routingKey, mandatory, serializedMessage.Properties, serializedMessage.Body, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync<TType, TMessage>(
        Exchange exchange,
        string routingKey,
        bool mandatory,
        TMessage message,
        CancellationToken cancellationToken
    ) where TMessage: IMessage<TType>
    {
        using var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
        await PublishAsync(
            exchange, routingKey, mandatory, serializedMessage.Properties, serializedMessage.Body, cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync(
        Exchange exchange,
        string routingKey,
        bool mandatory,
        MessageProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var rawMessage = produceConsumeInterceptors.OnProduce(new ProducedMessage(properties, body));

        if (configuration.PublisherConfirms)
        {
            while (true)
            {
                var pendingConfirmation = await persistentChannelDispatcher.InvokeAsync<IPublishPendingConfirmation, BasicPublishWithConfirmsAction>(
                    new BasicPublishWithConfirmsAction(confirmationListener, exchange, routingKey, mandatory, rawMessage),
                    PersistentChannelDispatchOptions.ProducerPublishWithConfirms,
                    cts.Token
                ).ConfigureAwait(false);

                try
                {
                    await pendingConfirmation.WaitAsync(cts.Token).ConfigureAwait(false);
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
                new BasicPublishAction(exchange, routingKey, mandatory, rawMessage),
                PersistentChannelDispatchOptions.ProducerPublish,
                cts.Token
            ).ConfigureAwait(false);
        }

        eventBus.Publish(
            new PublishedMessageEvent(exchange, routingKey, rawMessage.Properties, rawMessage.Body)
        );

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Published to exchange {exchange} with routingKey={routingKey} and correlationId={correlationId}",
                exchange.Name,
                routingKey,
                properties.CorrelationId
            );
        }
    }

    #endregion

    #region Exchage, Queue, Binding

    /// <inheritdoc />
    public Task<Queue> QueueDeclareAsync(CancellationToken cancellationToken)
    {
        return QueueDeclareAsync(
            string.Empty,
            c => c.AsDurable(true).AsExclusive(true).AsAutoDelete(true),
            cancellationToken
        );
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
        Action<IQueueDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var queueDeclareConfiguration = new QueueDeclareConfiguration();
        configure(queueDeclareConfiguration);
        var isDurable = queueDeclareConfiguration.IsDurable;
        var isExclusive = queueDeclareConfiguration.IsExclusive;
        var isAutoDelete = queueDeclareConfiguration.IsAutoDelete;
        var arguments = queueDeclareConfiguration.Arguments;

        var queueDeclareOk = await persistentChannelDispatcher.InvokeAsync(
            x => x.QueueDeclare(queue, isDurable, isExclusive, isAutoDelete, arguments),
            PersistentChannelDispatchOptions.ConsumerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Declared queue {queue}: durable={durable}, exclusive={exclusive}, autoDelete={autoDelete}, arguments={arguments}",
                queueDeclareOk.QueueName,
                isDurable,
                isExclusive,
                isAutoDelete,
                arguments?.Stringify()
            );
        }

        return new Queue(queueDeclareOk.QueueName, isDurable, isExclusive, isAutoDelete, arguments);
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
        Action<IExchangeDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var exchangeDeclareConfiguration = new ExchangeDeclareConfiguration();
        configure(exchangeDeclareConfiguration);
        var type = exchangeDeclareConfiguration.Type;
        var isDurable = exchangeDeclareConfiguration.IsDurable;
        var isAutoDelete = exchangeDeclareConfiguration.IsAutoDelete;
        var arguments = exchangeDeclareConfiguration.Arguments;

        await persistentChannelDispatcher.InvokeAsync(
            x => x.ExchangeDeclare(exchange, type, isDurable, isAutoDelete, arguments),
            PersistentChannelDispatchOptions.ProducerTopology,
            cts.Token
        ).ConfigureAwait(false);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Declared exchange {exchange}: type={type}, durable={durable}, autoDelete={autoDelete}, arguments={arguments}",
                exchange,
                type,
                isDurable,
                isAutoDelete,
                arguments?.Stringify()
            );
        }

        return new Exchange(exchange, type, isDurable, isAutoDelete, arguments);
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

    private readonly struct BasicPublishAction : IPersistentChannelAction<bool>
    {
        private readonly Exchange exchange;
        private readonly string routingKey;
        private readonly bool mandatory;
        private readonly ProducedMessage message;

        public BasicPublishAction(in Exchange exchange, string routingKey, bool mandatory, in ProducedMessage message)
        {
            this.exchange = exchange;
            this.routingKey = routingKey;
            this.mandatory = mandatory;
            this.message = message;
        }

        public bool Invoke(IModel model)
        {
            var basicProperties = model.CreateBasicProperties();
            message.Properties.CopyTo(basicProperties);
            model.BasicPublish(exchange.Name, routingKey, mandatory, basicProperties, message.Body);
            return true;
        }
    }

    private readonly struct BasicPublishWithConfirmsAction : IPersistentChannelAction<IPublishPendingConfirmation>
    {
        private readonly IPublishConfirmationListener confirmationListener;
        private readonly Exchange exchange;
        private readonly string routingKey;
        private readonly bool mandatory;
        private readonly ProducedMessage message;

        public BasicPublishWithConfirmsAction(
            IPublishConfirmationListener confirmationListener,
            in Exchange exchange,
            string routingKey,
            bool mandatory,
            in ProducedMessage message
        )
        {
            this.confirmationListener = confirmationListener;
            this.exchange = exchange;
            this.routingKey = routingKey;
            this.mandatory = mandatory;
            this.message = message;
        }

        public IPublishPendingConfirmation Invoke(IModel model)
        {
            var confirmation = confirmationListener.CreatePendingConfirmation(model);
            var basicProperties = model.CreateBasicProperties();
            message.Properties.SetConfirmationId(confirmation.Id)
                .CopyTo(basicProperties);
            try
            {
                model.BasicPublish(exchange.Name, routingKey, mandatory, basicProperties, message.Body);
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
