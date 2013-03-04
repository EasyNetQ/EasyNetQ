﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;
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

        private readonly IPersistentConnection connection;
        private readonly ConcurrentBag<SubscriptionAction> subscribeActions = new ConcurrentBag<SubscriptionAction>();

        public const bool NoAck = false;

        public RabbitAdvancedBus(
            IConnectionConfiguration connectionConfiguration,
            IConnectionFactory connectionFactory,
            SerializeType serializeType, 
            ISerializer serializer, 
            IConsumerFactory consumerFactory, 
            IEasyNetQLogger logger, 
            Func<string> getCorrelationId, 
            IConventions conventions)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(serializeType, "serializeType");
            Preconditions.CheckNotNull(serializer, "serializer");
            Preconditions.CheckNotNull(consumerFactory, "consumerFactory");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(getCorrelationId, "getCorrelationId");
            Preconditions.CheckNotNull(conventions, "conventions");

            this.connectionConfiguration = connectionConfiguration;
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

        public virtual void Subscribe<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            Subscribe(queue, (body, properties, messageRecievedInfo) =>
            {
                CheckMessageType<T>(properties);

                var messageBody = serializer.BytesToMessage<T>(body);
                var message = new Message<T>(messageBody);
                message.SetProperties(properties);
                return onMessage(message, messageRecievedInfo);
            });
        }

        public virtual void Subscribe(IQueue queue, Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            if (disposed)
            {
                throw new EasyNetQException("This bus has been disposed");
            }

            var subscriptionAction = new SubscriptionAction(queue.IsSingleUse);

            subscriptionAction.Action = () =>
            {
                var channel = connection.CreateModel();
                channel.ModelShutdown += (model, reason) => logger.DebugWrite("Model Shutdown for queue: '{0}'", queue.Name);

                queue.Visit(new TopologyBuilder(channel));

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

        private void AddSubscriptionAction(SubscriptionAction subscriptionAction)
        {
            if(subscriptionAction.IsMultiUse)
            {
                subscribeActions.Add(subscriptionAction);
            }

            try
            {
                subscriptionAction.Action();
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

        public virtual IAdvancedPublishChannel OpenPublishChannel()
        {
            return OpenPublishChannel(x => { });
        }

        public virtual IAdvancedPublishChannel OpenPublishChannel(Action<IChannelConfiguration> configure)
        {
            return new RabbitAdvancedPublishChannel(this, configure);
        }

        public virtual event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null) Connected();

            logger.DebugWrite("Re-creating subscribers");
            try
            {
                foreach (var subscribeAction in subscribeActions)
                {
                    subscribeAction.Action();
                }
            }
            catch (OperationInterruptedException operationInterruptedException)
            {
                logger.ErrorWrite("Re-creating subscribers failed: reason: '{0}'\n{1}", 
                    operationInterruptedException.Message, 
                    operationInterruptedException.ToString());
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
        public SubscriptionAction(bool isSingleUse)
        {
            IsSingleUse = isSingleUse;
            ClearAction();
        }

        public void ClearAction()
        {
            Action = () => { };
        }

        public Action Action { get; set; }
        public bool IsSingleUse { get; private set; }
        public bool IsMultiUse { get { return !IsSingleUse; } }
    }
}