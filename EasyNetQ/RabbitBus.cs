using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class RabbitBus : IBus, IRawByteBus
    {
        private readonly SerializeType serializeType;
        private readonly ISerializer serializer;
        private readonly IPersistentConnection connection;
        private readonly IConsumerFactory consumerFactory;

        private const string rpcExchange = "easy_net_q_rpc";
        private const bool noAck = false;

        // prefetchCount determines how many messages will be allowed in the local in-memory queue
        // setting to zero makes this infinite, but risks an out-of-memory exception.
        private const int prefetchCount = 1000; 

        public RabbitBus(
            SerializeType serializeType, 
            ISerializer serializer,
            IConsumerFactory consumerFactory, 
            ConnectionFactory connectionFactory,
            IEasyNetQLogger logger)
        {
            if(serializeType == null)
            {
                throw new ArgumentNullException("serializeType");
            }
            if(serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }
            if(consumerFactory == null)
            {
                throw new ArgumentNullException("consumerFactory");
            }
            if(connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            this.serializeType = serializeType;
            this.consumerFactory = consumerFactory;
            this.serializer = serializer;

            connection = new PersistentConnection(connectionFactory, logger);
            connection.Connected += OnConnected;
            connection.Disconnected += consumerFactory.ClearConsumers;
            connection.Disconnected += OnDisconnected;
        }

        public void Publish<T>(T message)
        {
            if(message == null)
            {
                throw new ArgumentNullException("message");
            }

            var typeName = serializeType(typeof (T));
            var messageBody = serializer.MessageToBytes(message);

            RawPublish(typeName, messageBody);
        }

        public void RawPublish(string typeName, byte[] messageBody)
        {
            if (!connection.IsConnected)
            {
                throw new EasyNetQException("Publish failed. No rabbit server connected.");
            }

            try
            {
                using (var channel = connection.CreateModel())
                {
                    DeclarePublishExchange(channel, typeName);

                    var defaultProperties = channel.CreateBasicProperties();
                    defaultProperties.SetPersistent(true);

                    channel.BasicPublish(
                        typeName,                   // exchange
                        typeName,                   // routingKey 
                        defaultProperties,          // basicProperties
                        messageBody);               // body
                }
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException exception)
            {
                throw new EasyNetQException("Publish Failed: '{0}'", exception.Message);
            }
        }

        private static void DeclarePublishExchange(IModel channel, string typeName)
        {
            channel.ExchangeDeclare(
                typeName,               // exchange
                ExchangeType.Direct,    // type
                true);                  // durable
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            var typeName = serializeType(typeof(T));
            var subscriptionQueue = string.Format("{0}_{1}", subscriptionId, typeName);

            Action subscribeAction = () =>
            {
                var channel = connection.CreateModel();
                DeclarePublishExchange(channel, typeName);

                channel.BasicQos(0, prefetchCount, false);

                var queue = channel.QueueDeclare(
                    subscriptionQueue,  // queue
                    true,               // durable
                    false,              // exclusive
                    false,              // autoDelete
                    null);              // arguments

                channel.QueueBind(
                    queue,              // queue
                    typeName,           // exchange
                    typeName);          // routingKey

                var consumer = consumerFactory.CreateConsumer(channel, 
                    (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                    {
                        var message = serializer.BytesToMessage<T>(body);
                        onMessage(message);
                    });

                channel.BasicConsume(
                    subscriptionQueue,      // queue
                    noAck,                  // noAck 
                    consumer.ConsumerTag,   // consumerTag
                    consumer);              // consumer
            };

            connection.AddSubscriptionAction(subscribeAction);
        }

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse)
        {
            if (onResponse == null)
            {
                throw new ArgumentNullException("onResponse");
            }
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (!connection.IsConnected)
            {
                throw new EasyNetQException("Publish failed. No rabbit server connected.");
            }

            var returnQueueName = "EasyNetQ_return_" + Guid.NewGuid().ToString();

            SubscribeToResponse(onResponse, returnQueueName);
            RequestPublish(request, returnQueueName);
        }

        private void SubscribeToResponse<TResponse>(Action<TResponse> onResponse, string returnQueueName)
        {
            var responseChannel = connection.CreateModel();

            // respond queue is transient, only exists for the lifetime of the call.
            var respondQueue = responseChannel.QueueDeclare(
                returnQueueName,
                false,              // durable
                true,               // exclusive
                true,               // autoDelete
                null                // arguments
                );

            var consumer = consumerFactory.CreateConsumer(responseChannel,
                (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                {
                    var response = serializer.BytesToMessage<TResponse>(body);
                    onResponse(response);

                    // close the response channel once we've consumed the message.
                    // this will also remove the queue.
                    // responseChannel.Close();
                });

            responseChannel.BasicConsume(
                respondQueue,           // queue
                noAck,                  // noAck 
                consumer.ConsumerTag,   // consumerTag
                consumer);              // consumer
        }

        private void RequestPublish<TRequest>(TRequest request, string returnQueueName)
        {
            var requestTypeName = serializeType(typeof(TRequest));
            var requestChannel = connection.CreateModel();

            // declare the exchange, binding and queue here. No need to set the mandatory flag, the recieving queue
            // will already have been declared, so in the case of no responder being present, message will collect
            // there.
            DeclareRequestResponseStructure(requestChannel, requestTypeName);

            // tell the consumer to respond to the transient respondQueue
            var requestProperties = requestChannel.CreateBasicProperties();
            requestProperties.ReplyTo = returnQueueName;

            var requestBody = serializer.MessageToBytes(request);
            requestChannel.BasicPublish(
                rpcExchange,            // exchange 
                requestTypeName,        // routingKey 
                requestProperties,      // basicProperties 
                requestBody);           // body
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            if(responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            Func<TRequest, Task<TResponse>> taskResponder = 
                request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

            RespondAsync(taskResponder);
        }

        public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        {
            if (responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            var requestTypeName = serializeType(typeof(TRequest));

            Action subscribeAction = () =>
            {
                var requestChannel = connection.CreateModel();
                DeclareRequestResponseStructure(requestChannel, requestTypeName);

                var consumer = consumerFactory.CreateConsumer(requestChannel,
                    (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                    {
                        var request = serializer.BytesToMessage<TRequest>(body);
                        var responseTask = responder(request);

                        responseTask.ContinueWith(task =>
                        {
                            // wait for the connection to come back
                            while (!connection.IsConnected) Thread.Sleep(100);

                            using(var responseChannel = connection.CreateModel())
                            {
                                var responseProperties = responseChannel.CreateBasicProperties();
                                var responseBody = serializer.MessageToBytes(task.Result);

                                responseChannel.BasicPublish(
                                    "",                 // exchange 
                                    properties.ReplyTo, // routingKey
                                    responseProperties, // basicProperties 
                                    responseBody);      // body
                            }
                        });
                    });

                // TODO: dispose channel
                requestChannel.BasicConsume(
                    requestTypeName,        // queue 
                    noAck,                   // noAck 
                    consumer.ConsumerTag,   // consumerTag
                    consumer);              // consumer
            };

            connection.AddSubscriptionAction(subscribeAction);
        }

        public void FuturePublish<T>(DateTime timeToRespond, T message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (!connection.IsConnected)
            {
                throw new EasyNetQException("FuturePublish failed. No rabbit server connected.");
            }

            var typeName = serializeType(typeof(T));
            var messageBody = serializer.MessageToBytes(message);

            Publish(new ScheduleMe
            {
                WakeTime = timeToRespond,
                BindingKey = typeName,
                InnerMessage = messageBody
            });
        }

        public event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null) Connected();
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

        private static void DeclareRequestResponseStructure(IModel channel, string requestTypeName)
        {
            channel.ExchangeDeclare(
                rpcExchange,            // exchange 
                ExchangeType.Direct,    // type 
                false,                  // autoDelete 
                true,                   // durable 
                null);                  // arguments

            channel.QueueDeclare(
                requestTypeName,    // queue 
                true,               // durable 
                false,              // exclusive 
                false,              // autoDelete 
                null);              // arguments

            channel.QueueBind(
                requestTypeName,    // queue
                rpcExchange,        // exchange 
                requestTypeName);   // routingKey
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            
            consumerFactory.Dispose();
            connection.Dispose();

            disposed = true;
        }
    }
}