using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
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
        protected readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        protected readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly ITimeoutStrategy timeoutStrategy;

        private readonly object responseQueuesAddLock = new object();
        private readonly ConcurrentDictionary<RpcKey, string> responseQueues = new ConcurrentDictionary<RpcKey, string>();
        private readonly ConcurrentDictionary<string, ResponseAction> responseActions = new ConcurrentDictionary<string, ResponseAction>();

        protected readonly TimeSpan disablePeriodicSignaling = TimeSpan.FromMilliseconds(-1);

        protected const string IsFaultedKey = "IsFaulted";
        protected const string ExceptionMessageKey = "ExceptionMessage";

        public Rpc(
            ConnectionConfiguration connectionConfiguration,
            IAdvancedBus advancedBus,
            IEventBus eventBus,
            IConventions conventions,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            ITimeoutStrategy timeoutStrategy)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "configuration");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(timeoutStrategy, "timeoutStrategy");

            this.connectionConfiguration = connectionConfiguration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.timeoutStrategy = timeoutStrategy;

            eventBus.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated);
        }

        private void OnConnectionCreated(ConnectionCreatedEvent @event)
        {
            var copyOfResponseActions = responseActions.Values;
            responseActions.Clear();
            responseQueues.Clear();
            
            // retry in-flight requests.
            foreach (var responseAction in copyOfResponseActions)
            {
                responseAction.OnFailure();
            }
        }

        public virtual Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();

            var tcs = new TaskCompletionSource<TResponse>();
            var timer = new Timer(state =>
                {
                    ((Timer) state).Dispose();
                    tcs.TrySetException(new TimeoutException(
                        string.Format("Request timed out. CorrelationId: {0}", correlationId.ToString())));
                });


            var requestType = typeof (TRequest);
            timer.Change(TimeSpan.FromSeconds(timeoutStrategy.GetTimeoutSeconds(requestType)), disablePeriodicSignaling);
            RegisterErrorHandling(correlationId, timer, tcs);

            var queueName = SubscribeToResponse<TRequest, TResponse>();
            var routingKey = conventions.RpcRoutingKeyNamingConvention(requestType);
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
                    timer.Dispose();

                    var msg = ((Message<TResponse>)message);

                    bool isFaulted = false;
                    string exceptionMessage = "The exception message has not been specified.";
                    if(msg.Properties.HeadersPresent)
                    {
                        if(msg.Properties.Headers.ContainsKey(IsFaultedKey))
                        {
                            isFaulted = Convert.ToBoolean(msg.Properties.Headers[IsFaultedKey]);
                        }
                        if(msg.Properties.Headers.ContainsKey(ExceptionMessageKey))
                        {
                            exceptionMessage = Encoding.UTF8.GetString((byte[])msg.Properties.Headers[ExceptionMessageKey]);
                        }
                    }

                    if(isFaulted)
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
                    timer.Dispose();
                    tcs.TrySetException(new EasyNetQException(
                        "Connection lost while request was in-flight. CorrelationId: {0}", correlationId.ToString()));
                }
            });
        }

        protected virtual string SubscribeToResponse<TRequest, TResponse>()
            where TResponse : class
        {
            var rpcKey = new RpcKey {Request = typeof (TRequest), Response = typeof (TResponse)};
            string queueName;
            if (responseQueues.TryGetValue(rpcKey, out queueName))
                return queueName;
            lock (responseQueuesAddLock)
            {
                if (responseQueues.TryGetValue(rpcKey, out queueName))
                    return queueName;
                var queue = advancedBus.QueueDeclare(
                            conventions.RpcReturnQueueNamingConvention(),
                            passive: false,
                            durable: false,
                            exclusive: true,
                            autoDelete: true);

                advancedBus.Consume<TResponse>(queue, (message, messageReceivedInfo) => Task.Factory.StartNew(() =>
                    {
                        ResponseAction responseAction;
                        if(responseActions.TryRemove(message.Properties.CorrelationId, out responseAction))
                        {
                            responseAction.OnSuccess(message);
                        }
                    }));
                responseQueues.TryAdd(rpcKey, queue.Name);
                return queue.Name;
            }
        }

        protected struct RpcKey
        {
            public Type Request;
            public Type Response;
        }

        protected class ResponseAction
        {
            public Action<object> OnSuccess { get; set; }
            public Action OnFailure { get; set; }
        }

        protected virtual void RequestPublish<TRequest>(TRequest request, string routingKey, string returnQueueName, Guid correlationId)
            where TRequest : class
        {
            var exchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);
            var requestType = typeof(TRequest);
            var requestMessage = new Message<TRequest>(request);
            requestMessage.Properties.ReplyTo = returnQueueName;
            requestMessage.Properties.CorrelationId = correlationId.ToString();
            requestMessage.Properties.Expiration = (timeoutStrategy.GetTimeoutSeconds(requestType) * 1000).ToString();
            requestMessage.Properties.DeliveryMode = (byte)(messageDeliveryModeStrategy.IsPersistent(requestType) ? 2 : 1);

            advancedBus.Publish(exchange, routingKey, false, false, requestMessage);
        }

        public virtual IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            return Respond(responder, c => { });
        }

        public IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure) where TRequest : class where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");
            Preconditions.CheckNotNull(configure, "configure");
            var configuration = new ResponderConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration);

            var routingKey = conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));

            var exchange = advancedBus.ExchangeDeclare(conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);
            var queue = advancedBus.QueueDeclare(routingKey);
            advancedBus.Bind(exchange, queue, routingKey);

            return advancedBus.Consume<TRequest>(queue, (requestMessage, messageRecievedInfo) => ExecuteResponder(responder, requestMessage),
                c => c.WithPrefetchCount(configuration.PrefetchCount));
        }

        protected Task ExecuteResponder<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IMessage<TRequest> requestMessage) 
            where TRequest : class 
            where TResponse : class
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                responder(requestMessage.Body).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        if (task.Exception != null)
                        {
                            OnResponderFailure<TRequest, TResponse>(requestMessage, task.Exception.InnerException.Message, task.Exception);
                            tcs.SetException(task.Exception);
                        }
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
            var responseMessage = new Message<TResponse>(response);
            responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;
            responseMessage.Properties.DeliveryMode = 1; // non-persistent

            advancedBus.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, false, false, responseMessage);
        }

        protected virtual void OnResponderFailure<TRequest, TResponse>(IMessage<TRequest> requestMessage, string exceptionMessage, Exception exception)
            where TRequest : class 
            where TResponse : class
        {
            var body = ReflectionHelpers.CreateInstance<TResponse>();
            var responseMessage = new Message<TResponse>(body);
            responseMessage.Properties.Headers.Add(IsFaultedKey, true);
            responseMessage.Properties.Headers.Add(ExceptionMessageKey, exceptionMessage);
            responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;
            responseMessage.Properties.DeliveryMode = 1; // non-persistent

            advancedBus.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, false, false, responseMessage);
        }
    }
}