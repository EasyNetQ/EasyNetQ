using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class RabbitAdvancedBus : IAdvancedBus
    {
        private readonly IConsumerFactory consumerFactory;
        private readonly IEasyNetQLogger logger;
        private readonly IPersistentConnection connection;
        private readonly IClientCommandDispatcher clientCommandDispatcher;
        private readonly IPublisher publisher;
        private readonly IEventBus eventBus;
        private readonly IHandlerCollectionFactory handlerCollectionFactory;
        private readonly IContainer container;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;

        public RabbitAdvancedBus(
            IConnectionFactory connectionFactory,
            IConsumerFactory consumerFactory,
            IEasyNetQLogger logger,
            IClientCommandDispatcherFactory clientCommandDispatcherFactory,
            IPublisher publisher,
            IEventBus eventBus,
            IHandlerCollectionFactory handlerCollectionFactory,
            IContainer container,
            IMessageSerializationStrategy messageSerializationStrategy)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(consumerFactory, "consumerFactory");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(publisher, "publisher");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(handlerCollectionFactory, "handlerCollectionFactory");
            Preconditions.CheckNotNull(container, "container");
            Preconditions.CheckNotNull(messageSerializationStrategy, "messageSerializationStrategy");

            this.consumerFactory = consumerFactory;
            this.logger = logger;
            this.publisher = publisher;
            this.eventBus = eventBus;
            this.handlerCollectionFactory = handlerCollectionFactory;
            this.container = container;
            this.messageSerializationStrategy = messageSerializationStrategy;

            connection = new PersistentConnection(connectionFactory, logger, eventBus);

            eventBus.Subscribe<ConnectionCreatedEvent>(e => OnConnected());
            eventBus.Subscribe<ConnectionDisconnectedEvent>(e => OnDisconnected());
            eventBus.Subscribe<ReturnedMessageEvent>(OnMessageReturned);

            clientCommandDispatcher = clientCommandDispatcherFactory.GetClientCommandDispatcher(connection);
        }



        // ---------------------------------- consume --------------------------------------

        public IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage) where T : class
        {
            return Consume<T>(queue, onMessage, x => { });
        }

        public IDisposable Consume<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage, Action<IConsumerConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return Consume<T>(queue, (message, info) => TaskHelpers.ExecuteSynchronously(() => onMessage(message, info)), configure);
        }

        public virtual IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage)
            where T : class
        {
            return Consume(queue, onMessage, x => { });
        }

        public IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return Consume(queue, x => x.Add(onMessage), configure);
        }

        public virtual IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers)
        {
            return Consume(queue, addHandlers, x => { });
        }

        public IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(addHandlers, "addHandlers");
            Preconditions.CheckNotNull(configure, "configure");

            var handlerCollection = handlerCollectionFactory.CreateHandlerCollection();
            addHandlers(handlerCollection);

            return Consume(queue, (body, properties, messageReceivedInfo) =>
            {
                var deserializedMessage = messageSerializationStrategy.DeserializeMessage(properties, body);
                var handler = handlerCollection.GetHandler(deserializedMessage.MessageType);
                return handler(deserializedMessage.Message, messageReceivedInfo);
            }, configure);
        }

        public IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            return Consume(queue, onMessage, x => { });
        }

        public virtual IDisposable Consume(IQueue queue, Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            if (disposed)
            {
                throw new EasyNetQException("This bus has been disposed");
            }
            var consumerConfiguration = new ConsumerConfiguration();
            configure(consumerConfiguration);
            var consumer = consumerFactory.CreateConsumer(queue, onMessage, connection, consumerConfiguration);
            return consumer.StartConsuming();
        }

        // -------------------------------- publish ---------------------------------------------

        public virtual Task PublishAsync(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            bool immediate,
            MessageProperties messageProperties,
            byte[] body)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(messageProperties, "messageProperties");
            Preconditions.CheckNotNull(body, "body");

            var task = clientCommandDispatcher.Invoke(x =>
                {
                    var properties = x.CreateBasicProperties();
                    messageProperties.CopyTo(properties);

                return publisher.Publish(x,
                        m => m.BasicPublish(exchange.Name, routingKey, mandatory, immediate, properties, body));
                }).Unwrap();

            logger.DebugWrite("Published to exchange: '{0}', routing key: '{1}', correlationId: '{2}'",
                exchange.Name, routingKey, messageProperties.CorrelationId);

            return task;
        }

        public virtual Task PublishAsync<T>(
            IExchange exchange,
            string routingKey,
            bool mandatory,
            bool immediate,
            IMessage<T> message) where T : class
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckShortString(routingKey, "routingKey");
            Preconditions.CheckNotNull(message, "message");

            var serializedMessage = messageSerializationStrategy.SerializeMessage(message);
            return PublishAsync(exchange, routingKey, mandatory, immediate, serializedMessage.Properties, serializedMessage.Body);
        }

        public void Publish(IExchange exchange, string routingKey, bool mandatory, bool immediate,
                                 MessageProperties messageProperties, byte[] body)
        {
            try
            {
                PublishAsync(exchange, routingKey, mandatory, immediate, messageProperties, body).Wait();
            }
            catch (AggregateException aggregateException)
            {
                throw aggregateException.InnerException;
            }
        }

        public void Publish<T>(IExchange exchange, string routingKey, bool mandatory, bool immediate, IMessage<T> message) where T : class
        {
            try
            {
                PublishAsync(exchange, routingKey, mandatory, immediate, message).Wait();
            }
            catch (AggregateException aggregateException)
            {
                throw aggregateException.InnerException;
            }
        }

        // ---------------------------------- Exchange / Queue / Binding -----------------------------------

        public virtual IQueue QueueDeclare(
            string name,
            bool passive = false,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            int perQueueTtl = int.MaxValue,
            int expires = int.MaxValue,
            string deadLetterExchange = null)
        {
            Preconditions.CheckNotNull(name, "name");

            IDictionary<string, object> arguments = new Dictionary<string, object>();
            if (passive)
            {
                clientCommandDispatcher.Invoke(
                    x => x.QueueDeclarePassive(name)).Wait();
            }
            else
            {
                if (perQueueTtl != int.MaxValue)
                {
                    arguments.Add("x-message-ttl", perQueueTtl);
                }

                if (expires != int.MaxValue)
                {
                    arguments.Add("x-expires", expires);
                }
                if (! string.IsNullOrEmpty(deadLetterExchange))
                {
                    arguments.Add("x-dead-letter-exchange", deadLetterExchange);
                }
                clientCommandDispatcher.Invoke(
                    x => x.QueueDeclare(name, durable, exclusive, autoDelete, arguments)
                    ).Wait();

                logger.DebugWrite("Declared Queue: '{0}' durable:{1}, exclusive:{2}, autoDelete:{3}, args:{4}",
                    name, durable, exclusive, autoDelete, WriteArguments(arguments));
            }

            return new Queue(name, exclusive);
        }

        private string WriteArguments(IEnumerable<KeyValuePair<string, object>> arguments)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var argument in arguments)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }
                builder.AppendFormat("{0}={1}", argument.Key, argument.Value);
            }
            return builder.ToString();
        }

        public virtual IQueue QueueDeclare()
        {
            var task = clientCommandDispatcher.Invoke(x => x.QueueDeclare());
            task.Wait();
            var queueDeclareOk = task.Result;
            logger.DebugWrite("Declared Server Generted Queue '{0}'", queueDeclareOk.QueueName);
            return new Queue(queueDeclareOk.QueueName, true);
        }

        public virtual void QueueDelete(IQueue queue, bool ifUnused = false, bool ifEmpty = false)
        {
            Preconditions.CheckNotNull(queue, "queue");

            clientCommandDispatcher.Invoke(x => x.QueueDelete(queue.Name, ifUnused, ifEmpty)).Wait();

            logger.DebugWrite("Deleted Queue: {0}", queue.Name);
        }

        public virtual void QueuePurge(IQueue queue)
        {
            Preconditions.CheckNotNull(queue, "queue");

            clientCommandDispatcher.Invoke(x => x.QueuePurge(queue.Name)).Wait();

            logger.DebugWrite("Purged Queue: {0}", queue.Name);
        }

        public virtual IExchange ExchangeDeclare(
            string name,
            string type,
            bool passive = false,
            bool durable = true,
            bool autoDelete = false,
            bool @internal = false,
            string alternateExchange = null)
        {
            Preconditions.CheckShortString(name, "name");
            Preconditions.CheckShortString(type, "type");

            if (passive)
            {
                clientCommandDispatcher.Invoke(x => x.ExchangeDeclarePassive(name)).Wait();
            }
            else
            {
                IDictionary<string, object> arguments = null;
                if (alternateExchange != null)
                {
                    arguments = new Dictionary<string, object> { { "alternate-exchange", alternateExchange } };
                }

                clientCommandDispatcher.Invoke(x => x.ExchangeDeclare(name, type, durable, autoDelete, arguments)).Wait();
                logger.DebugWrite("Declared Exchange: {0} type:{1}, durable:{2}, autoDelete:{3}",
                    name, type, durable, autoDelete);
            }

            return new Exchange(name);
        }

        public virtual void ExchangeDelete(IExchange exchange, bool ifUnused = false)
        {
            Preconditions.CheckNotNull(exchange, "exchange");

            clientCommandDispatcher.Invoke(x => x.ExchangeDelete(exchange.Name, ifUnused)).Wait();
            logger.DebugWrite("Deleted Exchange: {0}", exchange.Name);
        }

        public virtual IBinding Bind(IExchange exchange, IQueue queue, string routingKey)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckShortString(routingKey, "routingKey");

            clientCommandDispatcher.Invoke(x => x.QueueBind(queue.Name, exchange.Name, routingKey)).Wait();
            logger.DebugWrite("Bound queue {0} to exchange {1} with routing key {2}",
                queue.Name, exchange.Name, routingKey);
            return new Binding(queue, exchange, routingKey);
        }

        public virtual IBinding Bind(IExchange source, IExchange destination, string routingKey)
        {
            Preconditions.CheckNotNull(source, "source");
            Preconditions.CheckNotNull(destination, "destination");
            Preconditions.CheckShortString(routingKey, "routingKey");

            clientCommandDispatcher.Invoke(x => x.ExchangeBind(destination.Name, source.Name, routingKey)).Wait();

            logger.DebugWrite("Bound destination exchange {0} to source exchange {1} with routing key {2}",
                destination.Name, source.Name, routingKey);
            return new Binding(destination, source, routingKey);
        }

        public virtual void BindingDelete(IBinding binding)
        {
            Preconditions.CheckNotNull(binding, "binding");

            var queue = binding.Bindable as IQueue;
            if (queue != null)
            {
                clientCommandDispatcher.Invoke(
                    x => x.QueueUnbind(queue.Name, binding.Exchange.Name, binding.RoutingKey, null)
                    ).Wait();

                logger.DebugWrite("Unbound queue {0} from exchange {1} with routing key {2}",
                    queue.Name, binding.Exchange.Name, binding.RoutingKey);
            }
            else
            {
                var destination = binding.Bindable as IExchange;
                if (destination != null)
                {
                    clientCommandDispatcher.Invoke(
                        x => x.ExchangeUnbind(destination.Name, binding.Exchange.Name, binding.RoutingKey)
                        ).Wait();

                    logger.DebugWrite("Unbound destination exchange {0} from source exchange {1} with routing key {2}",
                        destination.Name, binding.Exchange.Name, binding.RoutingKey);
                }
            }
        }

        //------------------------------------------------------------------------------------------

        public virtual event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null) Connected();
        }

        public virtual event Action Disconnected;

        protected void OnDisconnected()
        {
            if (Disconnected != null) Disconnected();
        }

        public event Action<byte[], MessageProperties, MessageReturnedInfo> MessageReturned;

        protected void OnMessageReturned(ReturnedMessageEvent args)
        {
            if (MessageReturned != null) MessageReturned(args.Body, args.Properties, args.Info);
        }

        public virtual bool IsConnected
        {
            get { return connection.IsConnected; }
        }

        public IContainer Container
        {
            get { return container; }
        }

        private bool disposed = false;
        public virtual void Dispose()
        {
            if (disposed) return;

            consumerFactory.Dispose();
            clientCommandDispatcher.Dispose();
            connection.Dispose();

            disposed = true;

            logger.DebugWrite("Connection disposed");
        }
    }
}
