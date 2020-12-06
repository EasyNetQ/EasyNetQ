using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Interception;
using EasyNetQ.Internals;
using EasyNetQ.Logging;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <inheritdoc />
    public class RabbitAdvancedBus : IAdvancedBus
    {
        private readonly IClientCommandDispatcher clientCommandDispatcher;
        private readonly IPublishConfirmationListener confirmationListener;
        private readonly IPersistentConnection connection;
        private readonly ConnectionConfiguration configuration;
        private readonly IConsumerFactory consumerFactory;
        private readonly IEventBus eventBus;
        private readonly IHandlerCollectionFactory handlerCollectionFactory;
        private readonly ILog logger = LogProvider.For<RabbitAdvancedBus>();
        private readonly IMessageSerializationStrategy messageSerializationStrategy;
        private readonly IPullingConsumerFactory pullingConsumerFactory;
        private readonly IProduceConsumeInterceptor produceConsumeInterceptor;

        private bool disposed;
        private readonly IDisposable[] eventSubscriptions;

        /// <summary>
        ///     Creates RabbitAdvancedBus
        /// </summary>
        public RabbitAdvancedBus(
            IPersistentConnection connection,
            IConsumerFactory consumerFactory,
            IClientCommandDispatcher clientCommandDispatcher,
            IPublishConfirmationListener confirmationListener,
            IEventBus eventBus,
            IHandlerCollectionFactory handlerCollectionFactory,
            IServiceResolver container,
            ConnectionConfiguration configuration,
            IProduceConsumeInterceptor produceConsumeInterceptor,
            IMessageSerializationStrategy messageSerializationStrategy,
            IConventions conventions,
            IPullingConsumerFactory pullingConsumerFactory,
            AdvancedBusEventHandlers advancedBusEventHandlers
        )
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(consumerFactory, "consumerFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(handlerCollectionFactory, "handlerCollectionFactory");
            Preconditions.CheckNotNull(container, "container");
            Preconditions.CheckNotNull(messageSerializationStrategy, "messageSerializationStrategy");
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(produceConsumeInterceptor, "produceConsumeInterceptor");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(pullingConsumerFactory, "pullingConsumerFactory");
            Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");

            this.connection = connection;
            this.consumerFactory = consumerFactory;
            this.clientCommandDispatcher = clientCommandDispatcher;
            this.confirmationListener = confirmationListener;
            this.eventBus = eventBus;
            this.handlerCollectionFactory = handlerCollectionFactory;
            this.Container = container;
            this.configuration = configuration;
            this.produceConsumeInterceptor = produceConsumeInterceptor;
            this.messageSerializationStrategy = messageSerializationStrategy;
            this.pullingConsumerFactory = pullingConsumerFactory;
            this.Conventions = conventions;

            if (advancedBusEventHandlers.Connected != null)
                Connected += advancedBusEventHandlers.Connected;

            if (advancedBusEventHandlers.Disconnected != null)
                Disconnected += advancedBusEventHandlers.Disconnected;

            if (advancedBusEventHandlers.Blocked != null)
                Blocked += advancedBusEventHandlers.Blocked;

            if (advancedBusEventHandlers.Unblocked != null)
                Unblocked += advancedBusEventHandlers.Unblocked;

            if (advancedBusEventHandlers.MessageReturned != null)
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

        #region Consume

        /// <inheritdoc />
        public IDisposable Consume(IReadOnlyCollection<QueueConsumerPair> queueConsumerPairs, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queueConsumerPairs, nameof(queueConsumerPairs));
            Preconditions.CheckNotNull(configure, "configure");

            if (disposed)
                throw new EasyNetQException("This bus has been disposed");

            var queueOnMessages = queueConsumerPairs.Select(x =>
            {
                var onMessage = x.OnMessage;
                if (onMessage == null)
                {
                    var handlerCollection = handlerCollectionFactory.CreateHandlerCollection(x.Queue);
                    x.AddHandlers(handlerCollection);

                    onMessage = (b, p, i, c) =>
                    {
                        var deserializedMessage = messageSerializationStrategy.DeserializeMessage(p, b);
                        var handler = handlerCollection.GetHandler(deserializedMessage.MessageType);
                        return handler(deserializedMessage, i, c);
                    };
                }

                return Tuple.Create(x.Queue, onMessage);
            }).ToList();

            var consumerConfiguration = new ConsumerConfiguration(configuration.PrefetchCount);
            configure(consumerConfiguration);
            var consumer = consumerFactory.CreateConsumer(queueOnMessages, consumerConfiguration);
            consumer.StartConsuming();
            return consumer;
        }

        /// <inheritdoc />
        public IDisposable Consume<T>(
            IQueue queue, IMessageHandler<T> onMessage, Action<IConsumerConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return Consume(queue, x => x.Add(onMessage), configure);
        }

        /// <inheritdoc />
        public IDisposable Consume(
            IQueue queue, Action<IHandlerRegistration> addHandlers, Action<IConsumerConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(addHandlers, "addHandlers");
            Preconditions.CheckNotNull(configure, "configure");

            var handlerCollection = handlerCollectionFactory.CreateHandlerCollection(queue);
            addHandlers(handlerCollection);

            return Consume(queue, (body, properties, messageReceivedInfo, cancellationToken) =>
            {
                var deserializedMessage = messageSerializationStrategy.DeserializeMessage(properties, body);
                var handler = handlerCollection.GetHandler(deserializedMessage.MessageType);
                return handler(deserializedMessage, messageReceivedInfo, cancellationToken);
            }, configure);
        }

        /// <inheritdoc />
        public virtual IDisposable Consume(
            IQueue queue, MessageHandler onMessage, Action<IConsumerConfiguration> configure
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            if (disposed)
                throw new EasyNetQException("This bus has been disposed");

            var consumerConfiguration = new ConsumerConfiguration(configuration.PrefetchCount);
            configure(consumerConfiguration);
            var consumer = consumerFactory.CreateConsumer(queue, (body, properties, receivedInfo, cancellationToken) =>
            {
                var rawMessage = produceConsumeInterceptor.OnConsume(new ConsumedMessage(receivedInfo, properties, body));
                return onMessage(rawMessage.Body, rawMessage.Properties, receivedInfo, cancellationToken);
            }, consumerConfiguration);
            consumer.StartConsuming();
            return consumer;
        }

        #endregion

        #region Publish

        /// <inheritdoc />
        public virtual Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage message,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(message, "message");

            var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
            return PublishAsync(exchange, routingKey, mandatory, serializedMessage.Properties, serializedMessage.Body, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task PublishAsync<T>(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage<T> message,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(message, "message");

            var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
            return PublishAsync(exchange, routingKey, mandatory, serializedMessage.Properties, serializedMessage.Body, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            MessageProperties messageProperties,
            byte[] body,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(messageProperties, "messageProperties");
            Preconditions.CheckNotNull(body, "body");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var rawMessage = produceConsumeInterceptor.OnProduce(new ProducedMessage(messageProperties, body));

            if (configuration.PublisherConfirms)
            {
                while (true)
                {
                    var pendingConfirmation = await clientCommandDispatcher.InvokeAsync(model =>
                    {
                        var confirmation = confirmationListener.CreatePendingConfirmation(model);
                        rawMessage.Properties.SetConfirmationId(confirmation.Id);
                        var properties = model.CreateBasicProperties();
                        rawMessage.Properties.CopyTo(properties);
                        try
                        {
                            model.BasicPublish(exchange.Name, routingKey, mandatory, properties, rawMessage.Body);
                        }
                        catch (Exception)
                        {
                            confirmation.Cancel();
                            throw;
                        }
                        return confirmation;
                    }, ChannelDispatchOptions.PublishWithConfirms, cts.Token).ConfigureAwait(false);

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
                await clientCommandDispatcher.InvokeAsync(model =>
                {
                    var properties = model.CreateBasicProperties();
                    rawMessage.Properties.CopyTo(properties);
                    model.BasicPublish(exchange.Name, routingKey, mandatory, properties, rawMessage.Body);
                }, ChannelDispatchOptions.Publish, cts.Token).ConfigureAwait(false);
            }

            eventBus.Publish(new PublishedMessageEvent(exchange.Name, routingKey, rawMessage.Properties, rawMessage.Body));

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "Published to exchange {exchange} with routingKey={routingKey} and correlationId={correlationId}",
                    exchange.Name,
                    routingKey,
                    messageProperties.CorrelationId
                );
            }
        }

        #endregion

        #region Exchage, Queue, Binding

        /// <inheritdoc />
        public Task<IQueue> QueueDeclareAsync(CancellationToken cancellationToken)
        {
            return QueueDeclareAsync(
                string.Empty,
                c => c.AsDurable(true).AsExclusive(true).AsAutoDelete(true),
                cancellationToken
            );
        }

        /// <inheritdoc />
        public async Task QueueDeclarePassiveAsync(string name, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(name, "name");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            await clientCommandDispatcher.InvokeAsync(
                x => x.QueueDeclarePassive(name), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Passive declared queue {queue}", name);
            }
        }

        /// <inheritdoc />
        public async Task<IQueue> QueueDeclareAsync(
            string name,
            Action<IQueueDeclareConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(name, "name");
            Preconditions.CheckNotNull(configure, "configure");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var queueDeclareConfiguration = new QueueDeclareConfiguration();
            configure(queueDeclareConfiguration);
            var isDurable = queueDeclareConfiguration.IsDurable;
            var isExclusive = queueDeclareConfiguration.IsExclusive;
            var isAutoDelete = queueDeclareConfiguration.IsAutoDelete;
            var arguments = queueDeclareConfiguration.Arguments;

            var queueDeclareOk = await clientCommandDispatcher.InvokeAsync(
                x => x.QueueDeclare(name, isDurable, isExclusive, isAutoDelete, arguments), cts.Token
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
        public virtual async Task QueueDeleteAsync(IQueue queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queue, "queue");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            await clientCommandDispatcher.InvokeAsync(
                x => x.QueueDelete(queue.Name, ifUnused, ifEmpty), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Deleted queue {queue}", queue.Name);
            }
        }

        /// <inheritdoc />
        public virtual async Task QueuePurgeAsync(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            await clientCommandDispatcher.InvokeAsync(x => x.QueuePurge(queue.Name), cts.Token).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Purged queue {queue}", queue.Name);
            }
        }

        /// <inheritdoc />
        public async Task ExchangeDeclarePassiveAsync(string name, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckShortString(name, "name");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            await clientCommandDispatcher.InvokeAsync(
                x => x.ExchangeDeclarePassive(name), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Passive declared exchange {exchange}", name);
            }
        }

        /// <inheritdoc />
        public async Task<IExchange> ExchangeDeclareAsync(
            string name,
            Action<IExchangeDeclareConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckShortString(name, "name");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var exchangeDeclareConfiguration = new ExchangeDeclareConfiguration();
            configure(exchangeDeclareConfiguration);
            var type = exchangeDeclareConfiguration.Type;
            var isDurable = exchangeDeclareConfiguration.IsDurable;
            var isAutoDelete = exchangeDeclareConfiguration.IsAutoDelete;
            var arguments = exchangeDeclareConfiguration.Arguments;

            await clientCommandDispatcher.InvokeAsync(
                x => x.ExchangeDeclare(name, type, isDurable, isAutoDelete, arguments), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "Declared exchange {exchange}: type={type}, durable={durable}, autoDelete={autoDelete}, arguments={arguments}",
                    name,
                    type,
                    isDurable,
                    isAutoDelete,
                    arguments?.Stringify()
                );
            }

            return new Exchange(name, type, isDurable, isAutoDelete, arguments);
        }

        /// <inheritdoc />
        public virtual async Task ExchangeDeleteAsync(IExchange exchange, bool ifUnused = false, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchange, "exchange");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            await clientCommandDispatcher.InvokeAsync(
                x => x.ExchangeDelete(exchange.Name, ifUnused), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Deleted exchange {exchange}", exchange.Name);
            }
        }

        /// <inheritdoc />
        public async Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckShortString(routingKey, "routingKey");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            await clientCommandDispatcher.InvokeAsync(
                x => x.QueueBind(queue.Name, exchange.Name, routingKey, arguments),
                cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "Bound queue {queue} to exchange {exchange} with routingKey={routingKey} and arguments={arguments}",
                    queue.Name,
                    exchange.Name,
                    routingKey,
                    arguments?.Stringify()
                );
            }

            return new Binding(queue, exchange, routingKey, arguments);
        }

        /// <inheritdoc />
        public async Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(source, "source");
            Preconditions.CheckNotNull(destination, "destination");
            Preconditions.CheckShortString(routingKey, "routingKey");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            await clientCommandDispatcher.InvokeAsync(
                x => x.ExchangeBind(destination.Name, source.Name, routingKey, arguments),
                cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "Bound destination exchange {destinationExchange} to source exchange {sourceExchange} with routingKey={routingKey} and arguments={arguments}",
                    destination.Name,
                    source.Name,
                    routingKey,
                    arguments?.Stringify()
                );
            }

            return new Binding(destination, source, routingKey, arguments);
        }

        /// <inheritdoc />
        public virtual async Task UnbindAsync(IBinding binding, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(binding, "binding");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            if (binding.Bindable is IQueue queue)
            {
                await clientCommandDispatcher.InvokeAsync(
                    x => x.QueueUnbind(queue.Name, binding.Exchange.Name, binding.RoutingKey, null),
                    cts.Token
                ).ConfigureAwait(false);

                if (logger.IsDebugEnabled())
                {
                    logger.DebugFormat(
                        "Unbound queue {queue} from exchange {exchange} with routing key {routingKey}",
                        queue.Name,
                        binding.Exchange.Name,
                        binding.RoutingKey
                    );
                }
            }
            else if (binding.Bindable is IExchange destination)
            {
                await clientCommandDispatcher.InvokeAsync(
                    x => x.ExchangeUnbind(destination.Name, binding.Exchange.Name, binding.RoutingKey, null),
                    cts.Token
                ).ConfigureAwait(false);

                if (logger.IsDebugEnabled())
                {
                    logger.DebugFormat(
                        "Unbound destination exchange {destinationExchange} from source exchange {sourceExchange} with routing key {routingKey}",
                        destination.Name,
                        binding.Exchange.Name,
                        binding.RoutingKey
                    );
                }
            }
        }

        #endregion

        /// <inheritdoc />
        public async Task<QueueStats> GetQueueStatsAsync(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var declareResult = await clientCommandDispatcher.InvokeAsync(
                x => x.QueueDeclarePassive(queue.Name), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "{messagesCount} messages, {consumersCount} consumers in queue {queue}",
                    declareResult.MessageCount,
                    declareResult.ConsumerCount,
                    queue.Name
                );
            }

            return new QueueStats(declareResult.MessageCount, declareResult.ConsumerCount);
        }

        /// <inheritdoc />
        public IPullingConsumer<PullResult> CreatePullingConsumer(IQueue queue, bool autoAck = true)
        {
            var options = new PullingConsumerOptions(autoAck, configuration.Timeout);
            return pullingConsumerFactory.CreateConsumer(queue, options);
        }

        /// <inheritdoc />
        public IPullingConsumer<PullResult<T>> CreatePullingConsumer<T>(IQueue queue, bool autoAck = true)
        {
            var options = new PullingConsumerOptions(autoAck, configuration.Timeout);
            return pullingConsumerFactory.CreateConsumer<T>(queue, options);
        }

        /// <inheritdoc />
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <inheritdoc />
        public event EventHandler<BlockedEventArgs> Blocked;

        /// <inheritdoc />
        public event EventHandler Unblocked;

        /// <inheritdoc />
        public event EventHandler<MessageReturnedEventArgs> MessageReturned;

        /// <inheritdoc />
        public bool IsConnected => connection.IsConnected;

        /// <inheritdoc />
        public IServiceResolver Container { get; }

        /// <inheritdoc />
        public IConventions Conventions { get; }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if (disposed) return;

            foreach(var eventSubscription in eventSubscriptions)
                eventSubscription.Dispose();

            consumerFactory.Dispose();
            clientCommandDispatcher.Dispose();
            confirmationListener.Dispose();
            connection.Dispose();

            disposed = true;
        }

        private void OnConnectionCreated(ConnectionCreatedEvent @event)
        {
            Connected?.Invoke(this, new ConnectedEventArgs(@event.Endpoint.HostName, @event.Endpoint.Port));
        }

        private void OnConnectionRecovered(ConnectionRecoveredEvent @event)
        {
            Connected?.Invoke(this, new ConnectedEventArgs(@event.Endpoint.HostName, @event.Endpoint.Port));
        }

        private void OnConnectionDisconnected(ConnectionDisconnectedEvent @event)
        {
            Disconnected?.Invoke(
                this, new DisconnectedEventArgs(@event.Endpoint.HostName, @event.Endpoint.Port, @event.Reason)
            );
        }

        private void OnConnectionBlocked(ConnectionBlockedEvent @event)
        {
            Blocked?.Invoke(this, new BlockedEventArgs(@event.Reason));
        }

        private void OnConnectionUnblocked(ConnectionUnblockedEvent @event)
        {
            Unblocked?.Invoke(this, EventArgs.Empty);
        }

        private void OnMessageReturned(ReturnedMessageEvent @event)
        {
            MessageReturned?.Invoke(this, new MessageReturnedEventArgs(@event.Body, @event.Properties, @event.Info));
        }
    }
}
