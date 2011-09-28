using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly IEasyNetQLogger logger;
        private readonly Func<string> getCorrelationId; 

        private readonly IDictionary<int, string> responseQueueNameCache = new ConcurrentDictionary<int, string>();
        private readonly ISet<string> publishExchanges = new HashSet<string>(); 
        private readonly ISet<string> requestExchanges = new HashSet<string>();
        private List<IModel> modelList = new List<IModel>();

        private const string rpcExchange = "easy_net_q_rpc";
        private const bool noAck = false;

        // prefetchCount determines how many messages will be allowed in the local in-memory queue
        // setting to zero makes this infinite, but risks an out-of-memory exception.
        private const int prefetchCount = 1000; 

        public RabbitBus(
            SerializeType serializeType, 
            ISerializer serializer, 
            IConsumerFactory consumerFactory, 
            IConnectionFactory connectionFactory, 
            IEasyNetQLogger logger, 
            Func<string> getCorrelationId)
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
            if(getCorrelationId == null)
            {
                throw new ArgumentNullException("getCorrelationId");
            }

            this.serializeType = serializeType;
            this.consumerFactory = consumerFactory;
            this.logger = logger;
            this.serializer = serializer;
            this.getCorrelationId = getCorrelationId;

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

        // channels should not be shared between threads.
        private ThreadLocal<IModel> threadLocalPublishChannel = new ThreadLocal<IModel>(); 
        
        public void RawPublish(string typeName, byte[] messageBody)
        {
            if (!connection.IsConnected)
            {
                throw new EasyNetQException("Publish failed. No rabbit server connected.");
            }

            try
            {
                if (!threadLocalPublishChannel.IsValueCreated)
                {
                    threadLocalPublishChannel.Value = connection.CreateModel();
                }
                DeclarePublishExchange(threadLocalPublishChannel.Value, typeName);

                var defaultProperties = threadLocalPublishChannel.Value.CreateBasicProperties();
                defaultProperties.SetPersistent(false);
                defaultProperties.Type = typeName;
                defaultProperties.CorrelationId = getCorrelationId();

                threadLocalPublishChannel.Value.BasicPublish(
                    typeName, // exchange
                    typeName, // routingKey 
                    defaultProperties, // basicProperties
                    messageBody); // body

                logger.DebugWrite("Published {0}, CorrelationId {1}", typeName, defaultProperties.CorrelationId);
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException exception)
            {
                throw new EasyNetQException("Publish Failed: '{0}'", exception.Message);
            }
            catch (System.IO.IOException exception)
            {
                throw new EasyNetQException("Publish Failed: '{0}'", exception.Message);
            }
        }

        private void DeclarePublishExchange(IModel channel, string typeName)
        {
            // no need to declare on every publish
            if (!publishExchanges.Contains(typeName))
            {
                channel.ExchangeDeclare(
                    typeName,               // exchange
                    ExchangeType.Direct,    // type
                    true);                  // durable

                publishExchanges.Add(typeName);
            }
        }

        private void CheckMessageType<TMessage>(IBasicProperties properties)
        {
            var typeName = serializeType(typeof (TMessage));
            if (properties.Type != typeName)
            {
                logger.ErrorWrite("Message type is incorrect. Expected '{0}', but was '{1}'",
                    typeName, properties.Type);

                throw new EasyNetQInvalidMessageTypeException("Message type is incorrect. Expected '{0}', but was '{1}'",
                    typeName, properties.Type);
            }
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            SubscribeAsync<T>(subscriptionId, msg =>
            {
                var tcs = new TaskCompletionSource<object>();
                try
                {
                    onMessage(msg);
                    tcs.SetResult(null);
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }
                return tcs.Task;
            });
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
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
                modelList.Add( channel );
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
                        CheckMessageType<T>(properties);

                        var message = serializer.BytesToMessage<T>(body);
                        return onMessage(message);
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

            // rather than setting up a subscription on each call of Request, we cache a single
            // subscription keyed on the hashcode of the onResponse action. This has a couple of
            // consequences:
            //  1.  Closures don't work as expected since the closed over variable is always the first
            //      one that was called.
            //  2.  Worries about the uniqueness of MethodInfo.GetHashCode. Looking at the CLR source
            //      it seems that it's not overriden so it is the same as Object.GetHashCode(). This
            //      is unique for an instance in an app-domain, so it _should_ be OK for this usage.
            if (!responseQueueNameCache.ContainsKey(onResponse.Method.GetHashCode()))
            {
                logger.DebugWrite("Setting up return subscription for req/resp {0} {1}", 
                    typeof(TRequest).Name,
                    typeof(TResponse).Name);

                var uniqueResponseQueueName = "EasyNetQ_return_" + Guid.NewGuid().ToString();
                responseQueueNameCache.Add(onResponse.Method.GetHashCode(), uniqueResponseQueueName);
                SubscribeToResponse(onResponse, uniqueResponseQueueName);
            }

            var returnQueueName = responseQueueNameCache[onResponse.Method.GetHashCode()];

            RequestPublish(request, returnQueueName);
        }

        private void SubscribeToResponse<TResponse>(Action<TResponse> onResponse, string returnQueueName)
        {
            var responseChannel = connection.CreateModel();
            modelList.Add( responseChannel );

            // respond queue is transient, only exists for the lifetime of the service.
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
                    CheckMessageType<TResponse>(properties);
                    var response = serializer.BytesToMessage<TResponse>(body);

                    var tcs = new TaskCompletionSource<object>();

                    try
                    {
                        onResponse(response);
                        tcs.SetResult(null);
                    }
                    catch (Exception exception)
                    {
                        tcs.SetException(exception);
                    }
                    return tcs.Task;
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
            modelList.Add( requestChannel );

            // declare the exchange, binding and queue here. No need to set the mandatory flag, the recieving queue
            // will already have been declared, so in the case of no responder being present, message will collect
            // there.
            DeclareRequestResponseStructure(requestChannel, requestTypeName);

            // tell the consumer to respond to the transient respondQueue
            var requestProperties = requestChannel.CreateBasicProperties();
            requestProperties.ReplyTo = returnQueueName;
            requestProperties.Type = requestTypeName;

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
                modelList.Add( requestChannel );
                DeclareRequestResponseStructure(requestChannel, requestTypeName);

                var consumer = consumerFactory.CreateConsumer(requestChannel,
                    (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                    {
                        CheckMessageType<TRequest>(properties);
                        var request = serializer.BytesToMessage<TRequest>(body);
                        var responseTask = responder(request);

                        var tcs = new TaskCompletionSource<object>();
                        responseTask.ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                if (task.Exception != null)
                                {
                                    tcs.SetException(task.Exception);
                                }
                            }
                            else
                            {
                                // wait for the connection to come back
                                while (!connection.IsConnected) Thread.Sleep(100);

                                using(var responseChannel = connection.CreateModel())
                                {
                                    var responseProperties = responseChannel.CreateBasicProperties();
                                    responseProperties.Type = serializeType(typeof (TResponse));
                                    var responseBody = serializer.MessageToBytes(task.Result);

                                    responseChannel.BasicPublish(
                                        "",                 // exchange 
                                        properties.ReplyTo, // routingKey
                                        responseProperties, // basicProperties 
                                        responseBody);      // body
                                }
                                tcs.SetResult(null);
                            }
                        });
                        return tcs.Task;
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
            threadLocalPublishChannel.Dispose();
            threadLocalPublishChannel = new ThreadLocal<IModel>();

            publishExchanges.Clear();
            requestExchanges.Clear();
            responseQueueNameCache.Clear();
            if (Disconnected != null) Disconnected();
        }

        public bool IsConnected
        {
            get { return connection.IsConnected; }
        }

        private void DeclareRequestResponseStructure(IModel channel, string requestTypeName)
        {
            if (!requestExchanges.Contains(requestTypeName))
            {
                logger.DebugWrite("Declaring Request/Response structure for request: {0}", requestTypeName);

                channel.ExchangeDeclare(
                    rpcExchange, // exchange 
                    ExchangeType.Direct, // type 
                    false, // autoDelete 
                    true, // durable 
                    null); // arguments

                channel.QueueDeclare(
                    requestTypeName, // queue 
                    true, // durable 
                    false, // exclusive 
                    false, // autoDelete 
                    null); // arguments

                channel.QueueBind(
                    requestTypeName, // queue
                    rpcExchange, // exchange 
                    requestTypeName); // routingKey

                requestExchanges.Add(requestTypeName);
            }
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;

            // Abort all Models
            if ( modelList.Count > 0 ) {
                foreach ( var model in modelList ) {
                    if ( null != model )
                        model.Abort();
                }
            }

            threadLocalPublishChannel.Dispose();
            consumerFactory.Dispose();
            connection.Dispose();

            disposed = true;
        }
    }
}