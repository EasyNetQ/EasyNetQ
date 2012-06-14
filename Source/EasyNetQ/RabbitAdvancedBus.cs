using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    public class RabbitAdvancedBus : IAdvancedBus
    {
        private readonly SerializeType serializeType;
        private readonly ISerializer serializer;
        private readonly IConsumerFactory consumerFactory;
        private readonly IEasyNetQLogger logger;
        private readonly Func<string> getCorrelationId;
        private readonly IConventions conventions;

        private readonly IPersistentConnection connection;
        private readonly ConcurrentBag<Action> subscribeActions = new ConcurrentBag<Action>();

        public const bool NoAck = false;

        // prefetchCount determines how many messages will be allowed in the local in-memory queue
        // setting to zero makes this infinite, but risks an out-of-memory exception.
        // set to 50 based on this blog post:
        // http://www.rabbitmq.com/blog/2012/04/25/rabbitmq-performance-measurements-part-2/
        private const int prefetchCount = 50; 

        public RabbitAdvancedBus(
            IConnectionFactory connectionFactory,
            SerializeType serializeType, 
            ISerializer serializer, 
            IConsumerFactory consumerFactory, 
            IEasyNetQLogger logger, 
            Func<string> getCorrelationId, 
            IConventions conventions)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }
            if (serializeType == null)
            {
                throw new ArgumentNullException("serializeType");
            }
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }
            if (consumerFactory == null)
            {
                throw new ArgumentNullException("consumerFactory");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (getCorrelationId == null)
            {
                throw new ArgumentNullException("getCorrelationId");
            }
            if (conventions == null)
            {
                throw new ArgumentNullException("conventions");
            }

            this.serializeType = serializeType;
            this.serializer = serializer;
            this.consumerFactory = consumerFactory;
            this.logger = logger;
            this.getCorrelationId = getCorrelationId;
            this.conventions = conventions;

            connection = new PersistentConnection(connectionFactory, logger);
            connection.Connected += OnConnected;
            connection.Disconnected += consumerFactory.ClearConsumers;
            connection.Disconnected += OnDisconnected;
        }

        public SerializeType SerializeType
        {
            get { return serializeType; }
        }

        public ISerializer Serializer
        {
            get { return serializer; }
        }

        public IPersistentConnection Connection
        {
            get { return connection; }
        }

        public IConsumerFactory ConsumerFactory
        {
            get { return consumerFactory; }
        }

        public IEasyNetQLogger Logger
        {
            get { return logger; }
        }

        public Func<string> GetCorrelationId
        {
            get { return getCorrelationId; }
        }

        public IConventions Conventions
        {
            get { return conventions; }
        }

        public void Subscribe<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage)
        {
            if(queue == null)
            {
                throw new ArgumentNullException("queue");
            }
            if(onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            Subscribe(queue, (body, properties, messageRecievedInfo) =>
            {
                CheckMessageType<T>(properties);

                var messageBody = serializer.BytesToMessage<T>(body);
                var message = new Message<T>(messageBody);
                message.SetProperties(properties);
                return onMessage(message, messageRecievedInfo);
            });
        }

        public void Subscribe(IQueue queue, Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }
            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }
            if (disposed)
            {
                throw new EasyNetQException("This bus has been disposed");
            }

            Action subscribeAction = () =>
            {
                var channel = connection.CreateModel();
                channel.ModelShutdown += (model, reason) => Console.WriteLine("Model Shutdown for queue: '{0}'", queue.Name);

                queue.Visit(new TopologyBuilder(channel));

                channel.BasicQos(0, prefetchCount, false);

                var consumer = consumerFactory.CreateConsumer(channel, queue.IsSingleUse,
                    (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                    {
                        var messageRecievedInfo = new MessageReceivedInfo
                        {
                            ConsumerTag = consumerTag,
                            DeliverTag = deliveryTag,
                            Redelivered = redelivered,
                            Exchange = exchange,
                            RoutingKey = routingKey
                        };
                        var messsageProperties = new MessageProperties(properties);
                        return onMessage(body, messsageProperties, messageRecievedInfo);
                    });

                channel.BasicConsume(
                    queue.Name,             // queue
                    NoAck,                  // noAck 
                    consumer.ConsumerTag,   // consumerTag
                    consumer);              // consumer
            };

            AddSubscriptionAction(subscribeAction);
        }

        private void AddSubscriptionAction(Action subscriptionAction)
        {
            subscribeActions.Add(subscriptionAction);

            try
            {
                subscriptionAction();
            }
            catch (OperationInterruptedException)
            {

            }
            catch (EasyNetQException)
            {
                // Looks like the channel closed between our IsConnected check
                // and the subscription action. Do nothing here, when the 
                // connection comes back, the subcription action will be run then.
            }
        }

        private void CheckMessageType<TMessage>(MessageProperties properties)
        {
            var typeName = serializeType(typeof(TMessage));
            if (properties.Type != typeName)
            {
                logger.ErrorWrite("Message type is incorrect. Expected '{0}', but was '{1}'",
                    typeName, properties.Type);

                throw new EasyNetQInvalidMessageTypeException("Message type is incorrect. Expected '{0}', but was '{1}'",
                    typeName, properties.Type);
            }
        }

        public IAdvancedPublishChannel OpenPublishChannel()
        {
            return new RabbitAdvancedPublishChannel(this);
        }

        public event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null) Connected();

            logger.DebugWrite("Re-creating subscribers");
            foreach (var subscribeAction in subscribeActions)
            {
                subscribeAction();
            }
        }

        public event Action Disconnected;

        protected void OnDisconnected()
        {
            if (Disconnected != null) Disconnected();
        }

        public bool IsConnected
        {
            get { return connection.IsConnected; }
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;

            consumerFactory.Dispose();
            connection.Dispose();

            disposed = true;

            logger.DebugWrite("Connection disposed");
        }
    }
}