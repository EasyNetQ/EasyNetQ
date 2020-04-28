using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Default implementation of EasyNetQ's request-response pattern
    /// </summary>
    public class Rpc : IRpc
    {
        private readonly ConnectionConfiguration connectionConfiguration;
        protected readonly IAdvancedBus advancedBus;
        protected readonly IConventions conventions;
        protected readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        protected readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly ITimeoutStrategy timeoutStrategy;
        private readonly ITypeNameSerializer typeNameSerializer;

        private readonly object responseQueuesAddLock = new object();
        private readonly ConcurrentDictionary<RpcKey, ResponseQueueWithCancellation> responseQueues = new ConcurrentDictionary<RpcKey, ResponseQueueWithCancellation>();
        private readonly ConcurrentDictionary<string, ResponseAction> responseActions = new ConcurrentDictionary<string, ResponseAction>();

        protected readonly TimeSpan disablePeriodicSignaling = TimeSpan.FromMilliseconds(-1);

        protected const string isFaultedKey = "IsFaulted";
        protected const string exceptionMessageKey = "ExceptionMessage";

        public Rpc(
            ConnectionConfiguration connectionConfiguration,
            IAdvancedBus advancedBus,
            IEventBus eventBus,
            IConventions conventions,
            IExchangeDeclareStrategy exchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            ITimeoutStrategy timeoutStrategy,
            ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(connectionConfiguration, nameof(connectionConfiguration));
            Preconditions.CheckNotNull(advancedBus, nameof(advancedBus));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));
            Preconditions.CheckNotNull(conventions, nameof(conventions));
            Preconditions.CheckNotNull(exchangeDeclareStrategy, nameof(exchangeDeclareStrategy));
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, nameof(messageDeliveryModeStrategy));
            Preconditions.CheckNotNull(timeoutStrategy, nameof(timeoutStrategy));
            Preconditions.CheckNotNull(typeNameSerializer, nameof(typeNameSerializer));

            this.connectionConfiguration = connectionConfiguration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.exchangeDeclareStrategy = exchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.timeoutStrategy = timeoutStrategy;
            this.typeNameSerializer = typeNameSerializer;

            eventBus.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated);
        }

        private void OnConnectionCreated(ConnectionCreatedEvent @event)
        {
            var copyOfResponseQueues = responseQueues.Values;
            responseQueues.Clear();

            var copyOfResponseActions = responseActions.Values;
            responseActions.Clear();

            // call consumer cancellations
            foreach (var queueWithCancellation in copyOfResponseQueues)
            {
                queueWithCancellation.Cancellation.Dispose();
            }

            // finish in-flight requests
            foreach (var responseAction in copyOfResponseActions)
            {
                responseAction.OnFailure();
            }
        }

        public virtual Task<TResponse> Request<TRequest, TResponse>(TRequest request, Action<IRequestConfiguration> configure)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();
            var requestType = typeof(TRequest);
            var configuration = new RequestConfiguration();
            configure(configuration);

            var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            var timeout = timeoutStrategy.GetTimeoutSeconds(requestType);
            Timer timer = null;
            if (timeout > 0)
            {
                timer = new Timer(state =>
                {
                    tcs.TrySetException(new TimeoutException(string.Format("Request timed out. CorrelationId: {0}", correlationId.ToString())));
                    responseActions.TryRemove(correlationId.ToString(), out ResponseAction responseAction);
                }, null, TimeSpan.FromSeconds(timeout), disablePeriodicSignaling);
            }

            RegisterErrorHandling(correlationId, timer, tcs);

            var queueName = SubscribeToResponse<TRequest, TResponse>();
            var routingKey = configuration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);
            RequestPublish(request, routingKey, queueName, correlationId);

            return tcs.Task;
        }

        protected void RegisterErrorHandling<TResponse>(Guid correlationId, Timer timer, TaskCompletionSource<TResponse> tcs)
            where TResponse : class
        {
            responseActions.TryAdd(correlationId.ToString(), new ResponseAction
            {
                OnSuccess = message =>
                {
                    timer?.Dispose();

                    var msg = ((IMessage<TResponse>)message);

                    bool isFaulted = false;
                    string exceptionMessage = "The exception message has not been specified.";
                    if (msg.Properties.HeadersPresent)
                    {
                        if (msg.Properties.Headers.ContainsKey(isFaultedKey))
                        {
                            isFaulted = Convert.ToBoolean(msg.Properties.Headers[isFaultedKey]);
                        }
                        if (msg.Properties.Headers.ContainsKey(exceptionMessageKey))
                        {
                            exceptionMessage = Encoding.UTF8.GetString((byte[])msg.Properties.Headers[exceptionMessageKey]);
                        }
                    }

                    if (isFaulted)
                    {
                        tcs.TrySetException(new EasyNetQResponderException(exceptionMessage));
                    }
                    else
                    {
                        tcs.TrySetResult(msg.Body);
                    }
                },
                OnFailure = () =>
                {
                    timer?.Dispose();
                    tcs.TrySetException(new EasyNetQException("Connection lost while request was in-flight. CorrelationId: {0}", correlationId.ToString()));
                }
            });
        }

        protected virtual string SubscribeToResponse<TRequest, TResponse>()
            where TResponse : class
        {
            var responseType = typeof(TResponse);
            var rpcKey = new RpcKey { Request = typeof(TRequest), Response = responseType };
            if (responseQueues.TryGetValue(rpcKey, out ResponseQueueWithCancellation queueWithCancellation))
                return queueWithCancellation.QueueName;
            lock (responseQueuesAddLock)
            {
                if (responseQueues.TryGetValue(rpcKey, out queueWithCancellation))
                    return queueWithCancellation.QueueName;

                var queue = advancedBus.QueueDeclare(
                            conventions.RpcReturnQueueNamingConvention(),
                            passive: false,
                            durable: false,
                            exclusive: true,
                            autoDelete: true);

                var exchange = DeclareAndBindRpcExchange(
                    conventions.RpcResponseExchangeNamingConvention(responseType),
                    queue,
                    queue.Name);

                var cancellation = advancedBus.Consume<TResponse>(queue, (message, messageReceivedInfo) => Task.Factory.StartNew(() =>
                    {
                        ResponseAction responseAction;
                        if (responseActions.TryRemove(message.Properties.CorrelationId, out responseAction))
                        {
                            responseAction.OnSuccess(message);
                        }
                    }));
                responseQueues.TryAdd(rpcKey, new ResponseQueueWithCancellation { QueueName = queue.Name, Cancellation = cancellation });
                return queue.Name;
            }
        }

        protected struct RpcKey
        {
            public Type Request;
            public Type Response;
        }

        protected struct ResponseQueueWithCancellation
        {
            public string QueueName;
            public IDisposable Cancellation;
        }

        protected class ResponseAction
        {
            public Action<object> OnSuccess { get; set; }
            public Action OnFailure { get; set; }
        }

        protected virtual void RequestPublish<TRequest>(TRequest request, string routingKey, string returnQueueName, Guid correlationId)
            where TRequest : class
        {
            var requestType = typeof(TRequest);

            var exchange = DeclareRpcExchange(conventions.RpcRequestExchangeNamingConvention(requestType));

            var requestMessage = new Message<TRequest>(request)
            {
                Properties =
                {
                    ReplyTo = returnQueueName,
                    CorrelationId = correlationId.ToString(),
                    Expiration = (timeoutStrategy.GetTimeoutSeconds(requestType) * 1000).ToString(),
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(requestType)
                }
            };

            advancedBus.Publish(exchange, routingKey, false, requestMessage);
        }

        public virtual IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            return Respond(responder, c => { });
        }

        public virtual IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure) where TRequest : class where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");
            Preconditions.CheckNotNull(configure, "configure");
            // We're explicitly validating TResponse here because the type won't be used directly.
            // It'll only be used when executing a successful responder, which will silently fail if TResponse serialized length exceeds the limit.
            Preconditions.CheckShortString(typeNameSerializer.Serialize(typeof(TResponse)), "TResponse");

            var requestType = typeof(TRequest);

            var configuration = new ResponderConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration);

            var routingKey = configuration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);

            var queue = advancedBus.QueueDeclare(
                routingKey,
                durable: configuration.Durable,
                expires: configuration.Expires);

            var exchange = DeclareAndBindRpcExchange(
                    conventions.RpcRequestExchangeNamingConvention(requestType),
                    queue,
                    routingKey);

            return advancedBus.Consume<TRequest>(queue, (requestMessage, messageReceivedInfo) => ExecuteResponder(responder, requestMessage),
                c => c.WithPrefetchCount(configuration.PrefetchCount));
        }

        protected Task ExecuteResponder<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IMessage<TRequest> requestMessage)
            where TRequest : class
            where TResponse : class
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                responder(requestMessage.Body).ContinueWith(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        var exception = task.IsCanceled
                            ? new EasyNetQResponderException("The responder task was cancelled.")
                            : task.Exception?.InnerException ?? new EasyNetQResponderException("The responder faulted while dispatching the message.");


                        OnResponderFailure<TRequest, TResponse>(requestMessage, exception.Message, exception);
                        tcs.SetException(exception);
                    }
                    else
                    {
                        OnResponderSuccess(requestMessage, task.Result);
                        tcs.SetResult(null);
                    }
                });
            }
            catch (Exception e)
            {
                OnResponderFailure<TRequest, TResponse>(requestMessage, e.Message, e);
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        protected virtual void OnResponderSuccess<TRequest, TResponse>(IMessage<TRequest> requestMessage, TResponse response)
            where TRequest : class
            where TResponse : class
        {
            var responseMessage = new Message<TResponse>(response)
            {
                Properties =
                {
                    CorrelationId = requestMessage.Properties.CorrelationId,
                    DeliveryMode = MessageDeliveryMode.NonPersistent
                }
            };

            var exchange = DeclareRpcExchange(conventions.RpcResponseExchangeNamingConvention(typeof(TResponse)));

            advancedBus.Publish(exchange, requestMessage.Properties.ReplyTo, false, responseMessage);
        }

        protected virtual void OnResponderFailure<TRequest, TResponse>(IMessage<TRequest> requestMessage, string exceptionMessage, Exception exception)
            where TRequest : class
            where TResponse : class
        {
            var responseMessage = new Message<TResponse>();
            responseMessage.Properties.Headers.Add(isFaultedKey, true);
            responseMessage.Properties.Headers.Add(exceptionMessageKey, exceptionMessage);
            responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;
            responseMessage.Properties.DeliveryMode = MessageDeliveryMode.NonPersistent;

            var exchange = DeclareRpcExchange(conventions.RpcResponseExchangeNamingConvention(typeof(TResponse)));

            advancedBus.Publish(exchange, requestMessage.Properties.ReplyTo, false, responseMessage);
        }

        private IExchange DeclareRpcExchange(string exchangeName)
        {
            if (exchangeName != Exchange.GetDefault().Name)
            {
                return exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Direct);
            }
            else
            {
                return Exchange.GetDefault();
            }
        }

        private IExchange DeclareAndBindRpcExchange(string exchangeName, IQueue queue, string routingKey)
        {
            var exchange = DeclareRpcExchange(exchangeName);
            if (exchange != Exchange.GetDefault())
            {
                advancedBus.Bind(exchange, queue, routingKey);
            }
            return exchange;
        }
    }
}
