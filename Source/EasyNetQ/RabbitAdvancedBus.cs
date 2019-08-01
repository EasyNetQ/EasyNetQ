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
    public class RabbitAdvancedBus : IAdvancedBus
    {
        private readonly IClientCommandDispatcher clientCommandDispatcher;
        private readonly IPublishConfirmationListener confirmationListener;
        private readonly IPersistentConnection connection;
        private readonly ConnectionConfiguration connectionConfiguration;
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
            ConnectionConfiguration connectionConfiguration,
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
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(produceConsumeInterceptor, "produceConsumeInterceptor");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");

            this.connection = connection;
            this.consumerFactory = consumerFactory;
            this.confirmationListener = confirmationListener;
            this.eventBus = eventBus;
            this.handlerCollectionFactory = handlerCollectionFactory;
            this.Container = container;
            this.connectionConfiguration = connectionConfiguration;
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

        // ---------------------------------- consume --------------------------------------
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

            var consumerConfiguration = new ConsumerConfiguration(connectionConfiguration.PrefetchCount);
            configure(consumerConfiguration);
            var consumer = consumerFactory.CreateConsumer(queueOnMessages, consumerConfiguration);

            return consumer.StartConsuming();
        }

        public IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> onMessage, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return Consume(queue, x => x.Add(onMessage), configure);
        }

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

        public virtual IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            if (disposed)
                throw new EasyNetQException("This bus has been disposed");

            var consumerConfiguration = new ConsumerConfiguration(connectionConfiguration.PrefetchCount);
            configure(consumerConfiguration);
            var consumer = consumerFactory.CreateConsumer(queue, (body, properties, receivedInfo, cancellationToken) =>
            {
                var rawMessage = produceConsumeInterceptor.OnConsume(new RawMessage(properties, body));
                return onMessage(rawMessage.Body, rawMessage.Properties, receivedInfo, cancellationToken);
            }, consumerConfiguration);
            return consumer.StartConsuming();
        }

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

            // Fix me: It's very hard now to move publish logic to separate abstraction, just leave it here.
            var rawMessage = produceConsumeInterceptor.OnProduce(new RawMessage(messageProperties, body));
            if (connectionConfiguration.PublisherConfirms)
            {
                var timeout = TimeBudget.Start(TimeSpan.FromSeconds(connectionConfiguration.Timeout));
                while (!timeout.IsExpired())
                {
                    var confirmsWaiter = await clientCommandDispatcher.InvokeAsync(model =>
                    {
                        var properties = model.CreateBasicProperties();
                        rawMessage.Properties.CopyTo(properties);
                        var waiter = confirmationListener.GetWaiter(model);

                        try
                        {
                            model.BasicPublish(exchange.Name, routingKey, mandatory, properties, rawMessage.Body);
                        }
                        catch (Exception)
                        {
                            waiter.Cancel();
                            throw;
                        }

                        return waiter;
                    }, cancellationToken).ConfigureAwait(false);

                    try
                    {
                        await confirmsWaiter.WaitAsync(timeout).ConfigureAwait(false);
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
                }, cancellationToken).ConfigureAwait(false);
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


        // ---------------------------------- Exchange / Queue / Binding -----------------------------------


        public Task<IQueue> QueueDeclareAsync(CancellationToken cancellationToken)
        {
            return QueueDeclareAsync(
                string.Empty,
                c => c.AsDurable(true).AsExclusive(true).AsAutoDelete(true),
                cancellationToken
            );
        }

        public async Task QueueDeclarePassiveAsync(string name, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(name, "name");

            await clientCommandDispatcher.InvokeAsync(x => x.QueueDeclarePassive(name), cancellationToken).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Passive declared queue {queue}", name);
            }
        }

        public async Task<IQueue> QueueDeclareAsync(
            string name,
            Action<IQueueDeclareConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(name, "name");
            Preconditions.CheckNotNull(configure, "configure");

            var configuration = new QueueDeclareConfiguration();
            configure.Invoke(configuration);
            var isDurable = configuration.IsDurable;
            var isExclusive = configuration.IsExclusive;
            var isAutoDelete = configuration.IsAutoDelete;
            var arguments = configuration.Arguments;

            var queueDeclareOk = await clientCommandDispatcher.InvokeAsync(x => x.QueueDeclare(name, isDurable, isExclusive, isAutoDelete, arguments), cancellationToken)
                .ConfigureAwait(false);

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

        public virtual async Task QueueDeleteAsync(IQueue queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queue, "queue");

            await clientCommandDispatcher.InvokeAsync(x => x.QueueDelete(queue.Name, ifUnused, ifEmpty), cancellationToken).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Deleted queue {queue}", queue.Name);
            }
        }

        public virtual async Task QueuePurgeAsync(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            await clientCommandDispatcher.InvokeAsync(x => x.QueuePurge(queue.Name), cancellationToken).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Purged queue {queue}", queue.Name);
            }
        }

        public async Task ExchangeDeclarePassiveAsync(string name, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckShortString(name, "name");

            await clientCommandDispatcher.InvokeAsync(x => x.ExchangeDeclarePassive(name), cancellationToken).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Passive declared exchange {exchange}", name);
            }
        }

        public async Task<IExchange> ExchangeDeclareAsync(
            string name,
            Action<IExchangeDeclareConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckShortString(name, "name");

            var configuration = new ExchangeDeclareConfiguration();
            configure(configuration);
            var type = configuration.Type;
            var isDurable = configuration.IsDurable;
            var isAutoDelete = configuration.IsAutoDelete;
            var arguments = configuration.Arguments;

            await clientCommandDispatcher.InvokeAsync(x => x.ExchangeDeclare(name, type, isDurable, isAutoDelete, arguments), cancellationToken).ConfigureAwait(false);

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

        public virtual async Task ExchangeDeleteAsync(IExchange exchange, bool ifUnused = false, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchange, "exchange");

            await clientCommandDispatcher.InvokeAsync(x => x.ExchangeDelete(exchange.Name, ifUnused), cancellationToken).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Deleted exchange {exchange}", exchange.Name);
            }
        }

        public Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey, CancellationToken cancellationToken)
        {
            return BindAsync(exchange, queue, routingKey, new Dictionary<string, object>(), cancellationToken);
        }

        public async Task<IBinding> BindAsync(IExchange exchange, IQueue queue, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(arguments, "arguments");

            await clientCommandDispatcher.InvokeAsync(x => x.QueueBind(queue.Name, exchange.Name, routingKey, arguments), cancellationToken).ConfigureAwait(false);

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

        public Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey, CancellationToken cancellationToken)
        {
            return BindAsync(source, destination, routingKey, new Dictionary<string, object>(), cancellationToken);
        }

        public async Task<IBinding> BindAsync(IExchange source, IExchange destination, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(source, "source");
            Preconditions.CheckNotNull(destination, "destination");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(arguments, "arguments");

            await clientCommandDispatcher.InvokeAsync(x => x.ExchangeBind(destination.Name, source.Name, routingKey, arguments), cancellationToken).ConfigureAwait(false);

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

        public virtual async Task UnbindAsync(IBinding binding, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(binding, "binding");

            if (binding.Bindable is IQueue queue)
            {
                await clientCommandDispatcher.InvokeAsync(x => x.QueueUnbind(queue.Name, binding.Exchange.Name, binding.RoutingKey, null), cancellationToken).ConfigureAwait(false);

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
                await clientCommandDispatcher
                    .InvokeAsync(x => x.ExchangeUnbind(destination.Name, binding.Exchange.Name, binding.RoutingKey, null), cancellationToken)
                    .ConfigureAwait(false);

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
                return new BasicGetResult<T>(new Message<T>((T) message.GetBody(), message.Properties));
            }

            throw new EasyNetQException("Incorrect message type returned. Expected {0}, but was {1}", typeof(T).Name, message.MessageType.Name);
        }

        public async Task<IBasicGetResult> GetMessageAsync(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            var result = await clientCommandDispatcher.InvokeAsync(x => x.BasicGet(queue.Name, true), cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            var getResult = new BasicGetResult(
                result.Body,
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

        public async Task<uint> GetMessagesCountAsync(IQueue queue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");

            var declareResult = await clientCommandDispatcher.InvokeAsync(x => x.QueueDeclarePassive(queue.Name), cancellationToken).ConfigureAwait(false);

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("{messagesCount} messages in queue {queue}", declareResult.MessageCount, queue.Name);
            }

            return declareResult.MessageCount;
        }

        //------------------------------------------------------------------------------------------
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
            confirmationListener.Dispose();
            clientCommandDispatcher.Dispose();
            connection.Dispose();

            disposed = true;
        }

        protected void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);

        protected void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);

        protected void OnBlocked(ConnectionBlockedEvent args) => Blocked?.Invoke(this, new ConnectionBlockedEventArgs(args.Reason));

        protected void OnUnblocked() => Unblocked?.Invoke(this, EventArgs.Empty);

        protected void OnMessageReturned(ReturnedMessageEvent args) => MessageReturned?.Invoke(this, new MessageReturnedEventArgs(args.Body, args.Properties, args.Info));
    }
}
