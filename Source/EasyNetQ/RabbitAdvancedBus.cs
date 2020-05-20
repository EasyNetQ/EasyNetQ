using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Interception;
using EasyNetQ.Logging;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using RabbitMQ.Client.Events;

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
        private readonly IProduceConsumeInterceptor produceConsumeInterceptor;

        private bool disposed;

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
            Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");

            this.connection = connection;
            this.consumerFactory = consumerFactory;
            this.confirmationListener = confirmationListener;
            this.eventBus = eventBus;
            this.handlerCollectionFactory = handlerCollectionFactory;
            this.Container = container;
            this.configuration = configuration;
            this.produceConsumeInterceptor = produceConsumeInterceptor;
            this.messageSerializationStrategy = messageSerializationStrategy;
            this.Conventions = conventions;

            this.eventBus.Subscribe<ConnectionCreatedEvent>(e => OnConnected());
            if (advancedBusEventHandlers.Connected != null)
            {
                Connected += advancedBusEventHandlers.Connected;
            }

            this.eventBus.Subscribe<ConnectionDisconnectedEvent>(e => OnDisconnected());
            if (advancedBusEventHandlers.Disconnected != null)
            {
                Disconnected += advancedBusEventHandlers.Disconnected;
            }

            this.eventBus.Subscribe<ConnectionBlockedEvent>(OnBlocked);
            if (advancedBusEventHandlers.Blocked != null)
            {
                Blocked += advancedBusEventHandlers.Blocked;
            }

            this.eventBus.Subscribe<ConnectionUnblockedEvent>(e => OnUnblocked());
            if (advancedBusEventHandlers.Unblocked != null)
            {
                Unblocked += advancedBusEventHandlers.Unblocked;
            }

            this.eventBus.Subscribe<ReturnedMessageEvent>(OnMessageReturned);
            if (advancedBusEventHandlers.MessageReturned != null)
            {
                MessageReturned += advancedBusEventHandlers.MessageReturned;
            }

            this.clientCommandDispatcher = clientCommandDispatcher;
        }

        #region Consume

        /// <inheritdoc />
        public IDisposable Consume(IEnumerable<QueueConsumerPair> queueConsumerPairs, Action<IConsumerConfiguration> configure)
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

            return consumer.StartConsuming();
        }

        /// <inheritdoc />
        public IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> onMessage, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return Consume(queue, x => x.Add(onMessage), configure);
        }

        /// <inheritdoc />
        public IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers, Action<IConsumerConfiguration> configure)
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
        public virtual IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage, Action<IConsumerConfiguration> configure)
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
                var rawMessage = produceConsumeInterceptor.OnConsume(new RawMessage(properties, body));
                return onMessage(rawMessage.Body, rawMessage.Properties, receivedInfo, cancellationToken);
            }, consumerConfiguration);
            return consumer.StartConsuming();
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

            using var cts = CreateCancellationTokenSource(cancellationToken);

            var rawMessage = produceConsumeInterceptor.OnProduce(new RawMessage(messageProperties, body));

            if (configuration.PublisherConfirms)
            {
                while (true)
                {
                    var pendingConfirmation = await clientCommandDispatcher.InvokeAsync(model =>
                    {
                        var properties = model.CreateBasicProperties();
                        rawMessage.Properties.CopyTo(properties);
                        var confirmation = confirmationListener.CreatePendingConfirmation(model);
                        model.BasicPublish(exchange.Name, routingKey, mandatory, properties, rawMessage.Body);
                        return confirmation;
                    }, cts.Token).ConfigureAwait(false);

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
                }, cts.Token).ConfigureAwait(false);
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

            await clientCommandDispatcher.InvokeAsync(x => x.QueueDeclarePassive(name), cancellationToken).ConfigureAwait(false);

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

            using var cts = CreateCancellationTokenSource(cancellationToken);

            var queueDeclareConfiguration = new QueueDeclareConfiguration();
            configure.Invoke(queueDeclareConfiguration);
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
                    arguments.Stringify()
                );
            }

            return new Queue(queueDeclareOk.QueueName, isDurable, isExclusive, isAutoDelete, arguments);
        }

        /// <inheritdoc />
        public virtual async Task QueueDeleteAsync(IQueue queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queue, "queue");

            using var cts = CreateCancellationTokenSource(cancellationToken);

            await clientCommandDispatcher.InvokeAsync(x => x.QueueDelete(queue.Name, ifUnused, ifEmpty), cts.Token).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Deleted queue {queue}", queue.Name);
            }
        }

        /// <inheritdoc />
        public virtual async Task QueuePurgeAsync(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            using var cts = CreateCancellationTokenSource(cancellationToken);

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

            using var cts = CreateCancellationTokenSource(cancellationToken);

            await clientCommandDispatcher.InvokeAsync(x => x.ExchangeDeclarePassive(name), cts.Token).ConfigureAwait(false);

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

            using var cts = CreateCancellationTokenSource(cancellationToken);

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
                    arguments.Stringify()
                );
            }

            return new Exchange(name, type, isDurable, isAutoDelete, arguments);
        }

        /// <inheritdoc />
        public virtual async Task ExchangeDeleteAsync(IExchange exchange, bool ifUnused = false, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchange, "exchange");

            using var cts = CreateCancellationTokenSource(cancellationToken);

            await clientCommandDispatcher.InvokeAsync(
                x => x.ExchangeDelete(exchange.Name, ifUnused), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Deleted exchange {exchange}", exchange.Name);
            }
        }

        /// <inheritdoc />
        public Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey, CancellationToken cancellationToken)
        {
            return BindAsync(exchange, queue, routingKey, new Dictionary<string, object>(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(arguments, "arguments");

            using var cts = CreateCancellationTokenSource(cancellationToken);

            await clientCommandDispatcher.InvokeAsync(
                x => x.QueueBind(queue.Name, exchange.Name, routingKey, arguments), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "Bound queue {queue} to exchange {exchange} with routingKey={routingKey} and arguments={arguments}",
                    queue.Name,
                    exchange.Name,
                    routingKey,
                    arguments.Stringify()
                );
            }

            return new Binding(queue, exchange, routingKey, arguments);
        }

        /// <inheritdoc />
        public Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey, CancellationToken cancellationToken)
        {
            return BindAsync(source, destination, routingKey, new Dictionary<string, object>(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(source, "source");
            Preconditions.CheckNotNull(destination, "destination");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(arguments, "arguments");

            using var cts = CreateCancellationTokenSource(cancellationToken);

            await clientCommandDispatcher.InvokeAsync(
                x => x.ExchangeBind(destination.Name, source.Name, routingKey, arguments), cts.Token
            ).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "Bound destination exchange {destinationExchange} to source exchange {sourceExchange} with routingKey={routingKey} and arguments={arguments}",
                    destination.Name,
                    source.Name,
                    routingKey,
                    arguments.Stringify()
                );
            }

            return new Binding(destination, source, routingKey, arguments);
        }

        /// <inheritdoc />
        public virtual async Task UnbindAsync(IBinding binding, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(binding, "binding");

            using var cts = CreateCancellationTokenSource(cancellationToken);

            if (binding.Bindable is IQueue queue)
            {
                await clientCommandDispatcher.InvokeAsync(
                    x => x.QueueUnbind(queue.Name, binding.Exchange.Name, binding.RoutingKey, null), cts.Token
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
                    x => x.ExchangeUnbind(destination.Name, binding.Exchange.Name, binding.RoutingKey, null), cts.Token
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
        public async Task<IBasicGetResult<T>> GetMessageAsync<T>(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            var result = await GetMessageAsync(queue, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            var message = messageSerializationStrategy.DeserializeMessage(result.Properties, result.Body);
            if (typeof(T).IsAssignableFrom(message.MessageType))
            {
                return new BasicGetResult<T>(new Message<T>((T)message.GetBody(), message.Properties));
            }

            throw new EasyNetQException("Incorrect message type returned. Expected {0}, but was {1}", typeof(T).Name, message.MessageType.Name);
        }

        /// <inheritdoc />
        public async Task<IBasicGetResult> GetMessageAsync(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            using var cts = CreateCancellationTokenSource(cancellationToken);

            var result = await clientCommandDispatcher.InvokeAsync(x => x.BasicGet(queue.Name, true), cts.Token).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            var getResult = new BasicGetResult(
                result.Body.ToArray(),
                new MessageProperties(result.BasicProperties),
                new MessageReceivedInfo(
                    "",
                    result.DeliveryTag,
                    result.Redelivered,
                    result.Exchange,
                    result.RoutingKey,
                    queue.Name
                )
            );

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Got message from queue {queue}", queue.Name);
            }

            return getResult;
        }

        /// <inheritdoc />
        public async Task<uint> GetMessagesCountAsync(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            using var cts = CreateCancellationTokenSource(cancellationToken);

            var declareResult = await clientCommandDispatcher.InvokeAsync(x => x.QueueDeclarePassive(queue.Name), cts.Token).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("{messagesCount} messages in queue {queue}", declareResult.MessageCount, queue.Name);
            }

            return declareResult.MessageCount;
        }

        public virtual event EventHandler Connected;

        public virtual event EventHandler Disconnected;

        public virtual event EventHandler<ConnectionBlockedEventArgs> Blocked;

        public virtual event EventHandler Unblocked;

        public virtual event EventHandler<MessageReturnedEventArgs> MessageReturned;

        public virtual bool IsConnected => connection.IsConnected;

        public IServiceResolver Container { get; }

        public IConventions Conventions { get; }

        public virtual void Dispose()
        {
            if (disposed) return;

            consumerFactory.Dispose();
            clientCommandDispatcher.Dispose();
            confirmationListener.Dispose();
            connection.Dispose();

            disposed = true;
        }

        protected void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);

        protected void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);

        protected void OnBlocked(ConnectionBlockedEvent args) => Blocked?.Invoke(this, new ConnectionBlockedEventArgs(args.Reason));

        protected void OnUnblocked() => Unblocked?.Invoke(this, EventArgs.Empty);

        protected void OnMessageReturned(ReturnedMessageEvent args) => MessageReturned?.Invoke(this, new MessageReturnedEventArgs(args.Body, args.Properties, args.Info));

        private CancellationTokenSource CreateCancellationTokenSource(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if(configuration.Timeout != Timeout.InfiniteTimeSpan)
                cts.CancelAfter(configuration.Timeout);
            return cts;
        }
    }
}
