using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    public class RabbitAdvancedBus : IAdvancedBus
    {
        private readonly IConnectionConfiguration connectionConfiguration;
        private readonly SerializeType serializeType;
        private readonly ISerializer serializer;
        private readonly IConsumerFactory consumerFactory;
        private readonly IEasyNetQLogger logger;
        private readonly Func<string> getCorrelationId;
        private readonly IConventions conventions;
        private readonly IMessageValidationStrategy messageValidationStrategy;

        private readonly IPersistentConnection connection;
        private readonly ConcurrentDictionary<string, SubscriptionAction> subscribeActions = new ConcurrentDictionary<string, SubscriptionAction>();

        public const bool NoAck = false;

        public RabbitAdvancedBus(
            IConnectionConfiguration connectionConfiguration,
            IConnectionFactory connectionFactory,
            SerializeType serializeType, 
            ISerializer serializer, 
            IConsumerFactory consumerFactory, 
            IEasyNetQLogger logger, 
            Func<string> getCorrelationId, 
            IConventions conventions,
            IMessageValidationStrategy messageValidationStrategy)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(serializeType, "serializeType");
            Preconditions.CheckNotNull(serializer, "serializer");
            Preconditions.CheckNotNull(consumerFactory, "consumerFactory");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(getCorrelationId, "getCorrelationId");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(messageValidationStrategy, "messageValidationStrategy");

            this.connectionConfiguration = connectionConfiguration;
            this.serializeType = serializeType;
            this.serializer = serializer;
            this.consumerFactory = consumerFactory;
            this.logger = logger;
            this.getCorrelationId = getCorrelationId;
            this.conventions = conventions;
            this.messageValidationStrategy = messageValidationStrategy;

            connection = new PersistentConnection(connectionFactory, logger);
            connection.Connected += OnConnected;
            connection.Disconnected += consumerFactory.ClearConsumers;
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

            var newConsumerTag = conventions.ConsumerTagConvention();
            var subscriptionAction = new SubscriptionAction(newConsumerTag, logger, queue.IsSingleUse);

            subscriptionAction.Action = (isNewConnection) =>
            {
                // recreate channel if current channel is no longer open or connection was dropped and reconnected (to survive server restart)
                if (subscriptionAction.Channel == null || subscriptionAction.Channel.IsOpen == false || isNewConnection)
                {                    
                    subscriptionAction.Channel = CreateChannel(queue);
                }
                
                var channel = subscriptionAction.Channel;
                
                channel.BasicQos(0, connectionConfiguration.PrefetchCount, false);

                var consumer = consumerFactory.CreateConsumer(subscriptionAction, channel, queue.IsSingleUse,
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

                var cancelNotifications = consumer as IConsumerCancelNotifications;
                if (cancelNotifications != null)
                {
                    cancelNotifications.BasicCancel += OnBasicCancel;
                }

                channel.BasicConsume(
                    queue.Name,             // queue
                    NoAck,                  // noAck 
                    consumer.ConsumerTag,   // consumerTag
                    consumer);              // consumer

                logger.DebugWrite("Declared Consumer. queue='{0}', prefetchcount={1}",
                    queue.Name,
                    connectionConfiguration.PrefetchCount);
            };

            

            AddSubscriptionAction(subscriptionAction);
        }

        private IModel CreateChannel(IQueue queue)
        {
            var channel = connection.CreateModel();
            channel.ModelShutdown += (model, reason) => logger.DebugWrite("Model Shutdown for queue: '{0}'", queue.Name);
            return channel;
        }

        private void AddSubscriptionAction(SubscriptionAction subscriptionAction)
        {
            if(subscriptionAction.IsMultiUse)
            {
                if (!subscribeActions.TryAdd(subscriptionAction.Id, subscriptionAction))
                {
                    throw new EasyNetQException("Failed remember subscription action");
                }
            }

            subscriptionAction.ExecuteAction(true);
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
                }

                return Topology.Queue.Declare(durable, exclusive, autoDelete, name, arguments);
            }
        }

        public void QueueDelete(IQueue queue, bool ifUnused = false, bool ifEmpty = false)
        {
            using (var model = connection.CreateModel())
            {
                model.QueueDelete(queue.Name, ifUnused, ifEmpty);
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
                return new Exchange(name, type);
            }
        }

        public void ExchangeDelete(IExchange exchange, bool ifUnused = false)
        {
            using (var model = connection.CreateModel())
            {
                model.ExchangeDelete(exchange.Name, ifUnused);
            }
        }

        public IBinding Bind(IExchange exchange, IQueue queue, string routingKey)
        {
            using (var model = connection.CreateModel())
            {
                model.QueueBind(queue.Name, exchange.Name, routingKey);
                return new Binding(queue, exchange, routingKey);
            }
        }

        public IBinding Bind(IExchange source, IExchange destination, string routingKey)
        {
            using (var model = connection.CreateModel())
            {
                model.ExchangeBind(destination.Name, source.Name, routingKey);
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
                    model.QueueUnbind(queue.Name, binding.Exchange.Name, binding.RoutingKeys[0], null);
                }
                else
                {
                    var destination = binding.Bindable as IExchange;
                    if (destination != null)
                    {
                        model.ExchangeUnbind(destination.Name, binding.Exchange.Name, binding.RoutingKeys[0]);
                    }
                }
            }
        }

        //------------------------------------------------------------------------------------------

        public virtual event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null) Connected();

            logger.DebugWrite("Re-creating subscribers");

            foreach (var subscribeAction in subscribeActions.Values)
            {
                subscribeAction.ExecuteAction(true);
            }
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

        /// <summary>
        ///     Handles Consumer Cancel Notification: http://www.rabbitmq.com/consumer-cancel.html
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnBasicCancel(object sender, BasicCancelEventArgs args)
        {
            logger.InfoWrite("BasicCancel(Consumer Cancel Notification from broker) event received. Recreating queue and queue listener. Consumer tag: " + args.ConsumerTag);

            if (subscribeActions.ContainsKey(args.ConsumerTag))
            {
                // According to: http://www.rabbitmq.com/releases/rabbitmq-dotnet-client/v3.1.4/rabbitmq-dotnet-client-3.1.4-user-guide.pdf section 2.9.
                // All IBasicConsumer methods are dispatched by single background thread 
                // and MUST NOT invoke blocking AMQP operations: IModel.QueueDeclare, IModel.BasicCancel or IModel.BasicPublish...
                // For this reason we are recreating queues and queue listeners on separate thread.
                // Which is disposed after we are done.
                new Thread(() => subscribeActions[args.ConsumerTag].ExecuteAction(false)).Start();
            }
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

    public class SubscriptionAction
    {
        public SubscriptionAction(string id, IEasyNetQLogger logger, bool isSingleUse)
        {
            this.logger = logger;
            Id = id;
            IsSingleUse = isSingleUse;
            ClearAction();
        }

        private readonly IEasyNetQLogger logger;
        public string Id { get; private set; }
        public Action<bool> Action { get; set; }
        public IModel Channel { get; set; }
        public bool IsSingleUse { get; private set; }
        public bool IsMultiUse { get { return !IsSingleUse; } }

        public void ClearAction()
        {
            Action = (c) => { };
            Channel = null;
        }

        public void ExecuteAction(bool isNewConnection)
        {
            try
            {
                Action(isNewConnection);
            }
            catch (OperationInterruptedException operationInterruptedException)
            {
                logger.ErrorWrite("Failed to create subscribers: reason: '{0}'\n{1}",
                                  operationInterruptedException.Message,
                                  operationInterruptedException.ToString());
            }
            catch (EasyNetQException exc)
            {
                // and the subscription action."
                // Looks like the channel closed between our IsConnected check
                // and the subscription action. Do nothing here, when the 
                // connection comes back, the subscription action will be run then.
                logger.DebugWrite("Channel closed between our IsConnected check.", exc);
            }
        }
    }
}