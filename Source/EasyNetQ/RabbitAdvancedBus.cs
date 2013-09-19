using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class RabbitAdvancedBus : IAdvancedBus
    {
        private readonly SerializeType serializeType;
        private readonly ISerializer serializer;
        private readonly IConsumerFactory consumerFactory;
        private readonly IEasyNetQLogger logger;
        private readonly Func<string> getCorrelationId;
        private readonly IMessageValidationStrategy messageValidationStrategy;

        private readonly IPersistentConnection connection;

        public RabbitAdvancedBus(
            IConnectionFactory connectionFactory,
            SerializeType serializeType, 
            ISerializer serializer, 
            IConsumerFactory consumerFactory,
            IEasyNetQLogger logger, 
            Func<string> getCorrelationId, 
            IMessageValidationStrategy messageValidationStrategy)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(serializeType, "serializeType");
            Preconditions.CheckNotNull(serializer, "serializer");
            Preconditions.CheckNotNull(consumerFactory, "consumerFactory");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(getCorrelationId, "getCorrelationId");
            Preconditions.CheckNotNull(messageValidationStrategy, "messageValidationStrategy");

            this.serializeType = serializeType;
            this.serializer = serializer;
            this.consumerFactory = consumerFactory;
            this.logger = logger;
            this.getCorrelationId = getCorrelationId;
            this.messageValidationStrategy = messageValidationStrategy;

            connection = new PersistentConnection(connectionFactory, logger);
            connection.Connected += OnConnected;
            connection.Disconnected += OnDisconnected;
        }

        public virtual SerializeType SerializeType
        {
            get { return serializeType; }
        }

        public virtual ISerializer Serializer
        {
            get { return serializer; }
        }

        public IPersistentConnection Connection
        {
            get { return connection; }
        }

        public IEasyNetQLogger Logger
        {
            get { return logger; }
        }

        public Func<string> GetCorrelationId
        {
            get { return getCorrelationId; }
        }

        public virtual void Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            Consume(queue, (body, properties, messageRecievedInfo) =>
            {
                messageValidationStrategy.CheckMessageType<T>(body, properties, messageRecievedInfo);

                var messageBody = serializer.BytesToMessage<T>(body);
                var message = new Message<T>(messageBody);
                message.SetProperties(properties);
                return onMessage(message, messageRecievedInfo);
            });
        }
 
        public virtual void Consume(IQueue queue, Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {      
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            if (disposed)
            {
                throw new EasyNetQException("This bus has been disposed");
            }

            var consumer = consumerFactory.CreateConsumer(queue, onMessage, connection);
            consumer.StartConsuming();
        }

        public virtual IAdvancedPublishChannel OpenPublishChannel()
        {
            return OpenPublishChannel(x => { });
        }

        public virtual IAdvancedPublishChannel OpenPublishChannel(Action<IChannelConfiguration> configure)
        {
            return new RabbitAdvancedPublishChannel(this, configure);
        }

        // ---------------------------------- Exchange / Queue / Binding -----------------------------------

        public IQueue QueueDeclare(
            string name, 
            bool passive = false, 
            bool durable = true, 
            bool exclusive = false,
            bool autoDelete = false, 
            uint perQueueTtl = UInt32.MaxValue, 
            uint expires = UInt32.MaxValue)
        {
            using (var model = connection.CreateModel())
            {
                IDictionary<string, object> arguments = new Dictionary<string, object>();
                if (passive)
                {
                    model.QueueDeclarePassive(name);
                }
                else
                {
                    if (perQueueTtl != uint.MaxValue)
                    {
                        arguments.Add("x-message-ttl", perQueueTtl);
                    }

                    if (expires != uint.MaxValue)
                    {
                        arguments.Add("x-expires", expires);
                    }

                    model.QueueDeclare(name, durable, exclusive, autoDelete, (IDictionary)arguments);

                    logger.DebugWrite("Declared Queue: '{0}' durable:{1}, exclusive:{2}, autoDelte:{3}, args:{4}",
                        name, durable, exclusive, autoDelete, WriteArguments(arguments));
                }

                return new Topology.Queue(name, exclusive);
            }
        }

        private string WriteArguments(IEnumerable<KeyValuePair<string, object>> arguments)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var argument in arguments)
            {
                if(first)
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

        public IQueue QueueDeclare()
        {
            using (var model = connection.CreateModel())
            {
                var queueDeclareOk = model.QueueDeclare();
                logger.DebugWrite("Declared Server Generted Queue '{0}'", queueDeclareOk.QueueName);
                return new Topology.Queue(queueDeclareOk.QueueName, true);
            }
        }

        public void QueueDelete(IQueue queue, bool ifUnused = false, bool ifEmpty = false)
        {
            using (var model = connection.CreateModel())
            {
                model.QueueDelete(queue.Name, ifUnused, ifEmpty);
                logger.DebugWrite("Deleted Queue: {0}", queue.Name);
            }
        }

        public void QueuePurge(IQueue queue)
        {
            using (var model = connection.CreateModel())
            {
                model.QueuePurge(queue.Name);
                logger.DebugWrite("Purged Queue: {0}", queue.Name);
            }
        }

        public IExchange ExchangeDeclare(
            string name, 
            string type, 
            bool passive = false, 
            bool durable = true, 
            bool autoDelete = false,
            bool @internal = false)
        {
            using (var model = connection.CreateModel())
            {
                model.ExchangeDeclare(name, type, durable, autoDelete, null);
                logger.DebugWrite("Declared Exchange: {0} type:{1}, durable:{2}, autoDelete:{3}",
                    name, type, durable, autoDelete);
                return new Exchange(name);
            }
        }

        public void ExchangeDelete(IExchange exchange, bool ifUnused = false)
        {
            using (var model = connection.CreateModel())
            {
                model.ExchangeDelete(exchange.Name, ifUnused);
                logger.DebugWrite("Deleted Exchange: {0}", exchange.Name);
            }
        }

        public IBinding Bind(IExchange exchange, IQueue queue, string routingKey)
        {
            using (var model = connection.CreateModel())
            {
                model.QueueBind(queue.Name, exchange.Name, routingKey);
                logger.DebugWrite("Bound queue {0} to exchange {1} with routing key {2}",
                    queue.Name, exchange.Name, routingKey);
                return new Binding(queue, exchange, routingKey);
            }
        }

        public IBinding Bind(IExchange source, IExchange destination, string routingKey)
        {
            using (var model = connection.CreateModel())
            {
                model.ExchangeBind(destination.Name, source.Name, routingKey);
                logger.DebugWrite("Bound destination exchange {0} to source exchange {1} with routing key {2}",
                    destination.Name, source.Name, routingKey);
                return new Binding(destination, source, routingKey);
            }
        }

        public void BindingDelete(IBinding binding)
        {
            using (var model = connection.CreateModel())
            {
                var queue = binding.Bindable as IQueue;
                if (queue != null)
                {
                    model.QueueUnbind(queue.Name, binding.Exchange.Name, binding.RoutingKey, null);
                    logger.DebugWrite("Unbound queue {0} from exchange {1} with routing key {2}",
                        queue.Name, binding.Exchange.Name, binding.RoutingKey);
                }
                else
                {
                    var destination = binding.Bindable as IExchange;
                    if (destination != null)
                    {
                        model.ExchangeUnbind(destination.Name, binding.Exchange.Name, binding.RoutingKey);
                        logger.DebugWrite("Unbound destination exchange {0} from source exchange {1} with routing key {2}",
                            destination.Name, binding.Exchange.Name, binding.RoutingKey);
                    }
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

        public virtual bool IsConnected
        {
            get { return connection.IsConnected; }
        }

        private bool disposed = false;
        public virtual void Dispose()
        {
            if (disposed) return;

            consumerFactory.Dispose();
            connection.Dispose();

            disposed = true;

            logger.DebugWrite("Connection disposed");
        }
    }
}