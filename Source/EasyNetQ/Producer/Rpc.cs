﻿using System;
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
        protected readonly IAdvancedBus advancedBus;
        protected readonly IConventions conventions;
        protected readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        protected readonly IConnectionConfiguration configuration;

        private readonly ConcurrentDictionary<RpcKey, string> responseQueues = new ConcurrentDictionary<RpcKey, string>();
        private readonly ConcurrentDictionary<string, ResponseAction> responseActions = new ConcurrentDictionary<string, ResponseAction>();

        protected readonly TimeSpan disablePeriodicSignaling = TimeSpan.FromMilliseconds(-1);

        protected const string IsFaultedKey = "IsFaulted";
        protected const string ExceptionMessageKey = "ExceptionMessage";

        public Rpc(
            IAdvancedBus advancedBus,
            IEventBus eventBus,
            IConventions conventions,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.configuration = configuration;

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

            timer.Change(TimeSpan.FromSeconds(configuration.Timeout), disablePeriodicSignaling);

            RegisterErrorHandling(correlationId, timer, tcs);

            var queueName = SubscribeToResponse<TRequest, TResponse>();
            var routingKey = conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));
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

            responseQueues.AddOrUpdate(rpcKey,
                key =>
                    {
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

                        return queue.Name;
                    },
                (_, queueName) => queueName);

            return responseQueues[rpcKey];
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
            var exchange = publishExchangeDeclareStrategy.DeclareExchange(
                advancedBus, conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);

            var requestMessage = new Message<TRequest>(request);
            requestMessage.Properties.ReplyTo = returnQueueName;
            requestMessage.Properties.CorrelationId = correlationId.ToString();
            requestMessage.Properties.Expiration = (configuration.Timeout*1000).ToString();

            advancedBus.Publish(exchange, routingKey, false, false, requestMessage);
        }

        public virtual IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");

            var routingKey = conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));

            var exchange = advancedBus.ExchangeDeclare(conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);
            var queue = advancedBus.QueueDeclare(routingKey);
            advancedBus.Bind(exchange, queue, routingKey);

            return advancedBus.Consume<TRequest>(queue, (requestMessage, messageRecievedInfo) => ExecuteResponder(responder, requestMessage));
        }

        protected Task ExecuteResponder<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IMessage<TRequest> requestMessage) 
            where TRequest : class 
            where TResponse : class
        {
            var tcs = new TaskCompletionSource<object>();

            responder(requestMessage.Body).ContinueWith(task =>
            {
                if(task.IsFaulted)
                {
                    if(task.Exception != null)
                    {
                        var body = ReflectionHelpers.CreateInstance<TResponse>();
                        var responseMessage = new Message<TResponse>(body);
                        responseMessage.Properties.Headers.Add(IsFaultedKey, true);
                        responseMessage.Properties.Headers.Add(ExceptionMessageKey, task.Exception.InnerException.Message);
                        responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;

                        advancedBus.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, false, false, responseMessage);
                        tcs.SetException(task.Exception);
                    }
                }
                else
                {
                    var responseMessage = new Message<TResponse>(task.Result);
                    responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;

                    advancedBus.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, false, false, responseMessage);
                    tcs.SetResult(null);
                }
            });

            return tcs.Task;
        }
    }
}